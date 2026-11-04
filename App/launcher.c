/* Limbus Private Server - the Windows app.
 *
 * Starts the server out of sight (no console window), shows a small splash while it comes up,
 * and opens a plain Windows window (native controls, nothing custom): play here or on a
 * friend's server, host for friends, the mod menu (Edge as an app, on a profile of its own kept
 * off the network), the logs, GitHub. The server stops itself once the window is closed and
 * Limbus isn't running.
 *
 *   --restart     wait for the running server to stop, start it again, open nothing
 *   --no-window   start the server only (tests)
 *   --menu        the numbered menu in a console window (it also opens by itself once the patch
 *                 is attached); with its input and output redirected it reads and writes plain text
 *
 * Build (any OS):  zig rc /fo app.res app.rc
 *                  zig cc -target x86_64-windows-gnu -O2 -municode -Wl,--subsystem,windows launcher.c \
 *                      app.res -lws2_32 -lshell32 -ladvapi32 -luser32 -lgdi32 -o "Limbus Private Server.exe"
 */
#define WIN32_LEAN_AND_MEAN
#ifndef UNICODE
#define UNICODE
#endif
#ifndef _UNICODE
#define _UNICODE
#endif
#include <winsock2.h>
#include <windows.h>
#include <shellapi.h>
#include <commctrl.h>
#include <ctype.h>
#include <wchar.h>
#include <stdarg.h>
#include <stdio.h>
#include <stdlib.h>
#include <string.h>

#define PORT 3000
static const wchar_t *TITLE = L"Limbus Private Server";

static wchar_t here[MAX_PATH];                 /* this exe's folder, with a trailing backslash */
static HANDLE server_proc;                     /* the server we started, to notice it quitting */

static int exists(const wchar_t *path)
{
    DWORD a = GetFileAttributesW(path);
    return a != INVALID_FILE_ATTRIBUTES && !(a & FILE_ATTRIBUTE_DIRECTORY);
}

static void say(const wchar_t *text)
{
    MessageBoxW(NULL, text, TITLE, MB_OK | MB_ICONERROR | MB_SETFOREGROUND);
}

/* Something listens on 127.0.0.1:3000. A closed local port refuses at once, so no timeout. */
static int server_up(void)
{
    SOCKET s = socket(AF_INET, SOCK_STREAM, IPPROTO_TCP);
    if (s == INVALID_SOCKET)
        return 0;
    struct sockaddr_in a;
    ZeroMemory(&a, sizeof a);
    a.sin_family = AF_INET;
    a.sin_port = htons(PORT);
    a.sin_addr.s_addr = htonl(INADDR_LOOPBACK);
    int up = connect(s, (struct sockaddr *)&a, sizeof a) == 0;
    closesocket(s);
    return up;
}

static int server_quit(void)
{
    return server_proc && WaitForSingleObject(server_proc, 0) == WAIT_OBJECT_0;
}

static int wait_for(int up, int seconds)
{
    for (int i = 0; i < seconds * 4; i++) {
        if (server_up() == up)
            return 1;
        if (up && server_quit())
            return 0;
        Sleep(250);
    }
    return 0;
}

/* 1 started, 0 Windows refused, -1 not there */
static int start_server(void)
{
    wchar_t exe[MAX_PATH], dir[MAX_PATH], cmd[MAX_PATH + 2];
    if (_snwprintf(dir, MAX_PATH, L"%lsserver", here) < 0
        || _snwprintf(exe, MAX_PATH, L"%lsserver\\Server.exe", here) < 0
        || _snwprintf(cmd, MAX_PATH + 2, L"\"%ls\"", exe) < 0)
        return -1;
    if (!exists(exe))
        return -1;
    STARTUPINFOW si;
    PROCESS_INFORMATION pi;
    ZeroMemory(&si, sizeof si);
    si.cb = sizeof si;
    if (!CreateProcessW(exe, cmd, NULL, NULL, FALSE, CREATE_NO_WINDOW, NULL, dir, &si, &pi))
        return 0;
    CloseHandle(pi.hThread);
    server_proc = pi.hProcess;
    return 1;
}

/* The server couldn't read Limbus's own data (never started on this PC, say): it leaves the
 * reason in server\game-data-error.txt. Shows it and returns 1, or 0 if there is none. */
static int game_data_error(void)
{
    wchar_t path[MAX_PATH];
    if (_snwprintf(path, MAX_PATH, L"%lsserver\\game-data-error.txt", here) < 0)
        return 0;
    HANDLE f = CreateFileW(path, GENERIC_READ, FILE_SHARE_READ, NULL, OPEN_EXISTING, 0, NULL);
    if (f == INVALID_HANDLE_VALUE)
        return 0;
    char buf[2048];
    DWORD n = 0;
    ReadFile(f, buf, sizeof buf - 1, &n, NULL);
    CloseHandle(f);
    wchar_t text[2048];
    int len = MultiByteToWideChar(CP_UTF8, 0, buf, (int)n, text, 2047);
    if (len <= 0)
        return 0;
    text[len] = 0;
    MessageBoxW(NULL, text, TITLE, MB_OK | MB_ICONINFORMATION | MB_SETFOREGROUND);
    return 1;
}

static int find_edge(wchar_t *out, DWORD cap)
{
    static const wchar_t *key = L"SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\App Paths\\msedge.exe";
    HKEY roots[] = {HKEY_LOCAL_MACHINE, HKEY_CURRENT_USER};
    for (int i = 0; i < 2; i++) {
        DWORD size = cap * sizeof(wchar_t);
        if (RegGetValueW(roots[i], key, NULL, RRF_RT_REG_SZ, NULL, out, &size) == ERROR_SUCCESS && exists(out))
            return 1;
    }
    static const wchar_t *bases[] = {L"ProgramFiles(x86)", L"ProgramFiles", L"LOCALAPPDATA"};
    for (int i = 0; i < 3; i++) {
        wchar_t base[MAX_PATH];
        DWORD n = GetEnvironmentVariableW(bases[i], base, MAX_PATH);
        if (n == 0 || n >= MAX_PATH)
            continue;
        if (_snwprintf(out, cap, L"%ls\\Microsoft\\Edge\\Application\\msedge.exe", base) > 0 && exists(out))
            return 1;
    }
    return 0;
}

/* ---- the splash: icon, name and a thin line while the server starts, in the theme's colours ---- */

static HWND splash;
static HFONT font_title, font_sub;
static HICON splash_icon;
static int splash_tick, dpi = 96;
static COLORREF col_bg = RGB(0, 0, 0), col_fg = RGB(0xf2, 0xf2, 0xf2), col_accent = RGB(0x7f, 0xa7, 0xd4);
#define S(x) MulDiv((x), dpi, 96)

static void fill(HDC dc, int l, int t, int r, int b, COLORREF c)
{
    RECT rc = {l, t, r, b};
    HBRUSH br = CreateSolidBrush(c);
    FillRect(dc, &rc, br);
    DeleteObject(br);
}

static COLORREF mix(COLORREF a, COLORREF b, int pct_a)
{
    return RGB((GetRValue(a) * pct_a + GetRValue(b) * (100 - pct_a)) / 100,
               (GetGValue(a) * pct_a + GetGValue(b) * (100 - pct_a)) / 100,
               (GetBValue(a) * pct_a + GetBValue(b) * (100 - pct_a)) / 100);
}

/* "#rrggbb" after "key":" in the theme file, or `fallback`. */
static COLORREF theme_hex(const char *text, const char *key, COLORREF fallback)
{
    char pat[32];
    _snprintf(pat, sizeof pat, "\"%s\":\"#", key);
    const char *at = strstr(text, pat);
    unsigned v;
    if (!at || sscanf(at + strlen(pat), "%6x", &v) != 1)
        return fallback;
    return RGB((v >> 16) & 255, (v >> 8) & 255, v & 255);
}

/* The mod menu's Appearance setting (server\theme.json): background and accent. Black if unset. */
static void splash_theme(void)
{
    wchar_t path[MAX_PATH];
    if (_snwprintf(path, MAX_PATH, L"%lsserver\\theme.json", here) < 0)
        return;
    HANDLE f = CreateFileW(path, GENERIC_READ, FILE_SHARE_READ, NULL, OPEN_EXISTING, 0, NULL);
    if (f == INVALID_HANDLE_VALUE)
        return;
    char text[1024];
    DWORD n = 0;
    ReadFile(f, text, sizeof text - 1, &n, NULL);
    CloseHandle(f);
    text[n] = 0;
    if (strstr(text, "\"bg\":\"white\"") || (strstr(text, "\"bg\":\"picture\"") && strstr(text, "\"tone\":\"light\"")))
        col_bg = RGB(255, 255, 255);
    else if (strstr(text, "\"bg\":\"colour\""))
        col_bg = theme_hex(text, "colour", col_bg);
    int light = GetRValue(col_bg) * 299 + GetGValue(col_bg) * 587 + GetBValue(col_bg) * 114 > 150000;
    col_fg = light ? RGB(0x11, 0x11, 0x11) : RGB(0xf2, 0xf2, 0xf2);
    col_accent = theme_hex(text, "accent", col_accent);
}

static void splash_draw(HDC dc, RECT rc)
{
    fill(dc, 0, 0, rc.right, rc.bottom, mix(col_fg, col_bg, 18));                    /* hairline */
    fill(dc, 1, 1, rc.right - 1, rc.bottom - 1, col_bg);
    if (splash_icon)
        DrawIconEx(dc, S(24), S(28), splash_icon, S(32), S(32), 0, NULL, DI_NORMAL);
    SetBkMode(dc, TRANSPARENT);
    SelectObject(dc, font_title);
    SetTextColor(dc, col_fg);
    TextOutW(dc, S(70), S(26), TITLE, (int)wcslen(TITLE));
    SelectObject(dc, font_sub);
    SetTextColor(dc, mix(col_fg, col_bg, 60));
    TextOutW(dc, S(70), S(48), L"Starting the server", 19);
    /* a thin line along the bottom, a short stretch of accent moving slowly along it */
    int l = 1, r = rc.right - 1, t = rc.bottom - S(3), b = rc.bottom - 1;
    fill(dc, l, t, r, b, mix(col_fg, col_bg, 10));
    int seg = (r - l) / 4, pos = (splash_tick * S(2)) % (r - l + seg) - seg;
    int bl = l + pos < l ? l : l + pos, br = l + pos + seg > r ? r : l + pos + seg;
    if (br > bl)
        fill(dc, bl, t, br, b, col_accent);
}

static LRESULT CALLBACK splash_proc(HWND w, UINT msg, WPARAM wp, LPARAM lp)
{
    switch (msg) {
    case WM_TIMER:
        splash_tick++;
        InvalidateRect(w, NULL, FALSE);
        return 0;
    case WM_ERASEBKGND:
        return 1;
    case WM_NCHITTEST:
        return HTCAPTION;                      /* drag it anywhere */
    case WM_PAINT: {
        PAINTSTRUCT ps;
        HDC dc = BeginPaint(w, &ps);
        RECT rc;
        GetClientRect(w, &rc);
        HDC mem = CreateCompatibleDC(dc);
        HBITMAP bmp = CreateCompatibleBitmap(dc, rc.right, rc.bottom);
        HGDIOBJ old = SelectObject(mem, bmp);
        splash_draw(mem, rc);
        BitBlt(dc, 0, 0, rc.right, rc.bottom, mem, 0, 0, SRCCOPY);
        SelectObject(mem, old);
        DeleteObject(bmp);
        DeleteDC(mem);
        EndPaint(w, &ps);
        return 0;
    }
    }
    return DefWindowProcW(w, msg, wp, lp);
}

static void splash_open(HINSTANCE inst)
{
    HDC screen = GetDC(NULL);
    if (screen) {
        dpi = GetDeviceCaps(screen, LOGPIXELSY);
        ReleaseDC(NULL, screen);
    }
    WNDCLASSW wc;
    ZeroMemory(&wc, sizeof wc);
    wc.lpfnWndProc = splash_proc;
    wc.hInstance = inst;
    wc.hCursor = LoadCursorW(NULL, IDC_APPSTARTING);
    wc.hIcon = LoadIconW(inst, MAKEINTRESOURCEW(1));
    wc.lpszClassName = L"LimbusPrivateServerSplash";
    if (!RegisterClassW(&wc))
        return;
    splash_theme();
    int w = S(380), h = S(92);
    splash = CreateWindowExW(WS_EX_APPWINDOW, wc.lpszClassName, TITLE, WS_POPUP,
                             (GetSystemMetrics(SM_CXSCREEN) - w) / 2, (GetSystemMetrics(SM_CYSCREEN) - h) / 2,
                             w, h, NULL, NULL, inst, NULL);
    if (!splash)
        return;                                /* no display to show it on: carry on without */
    font_title = CreateFontW(-S(15), 0, 0, 0, FW_SEMIBOLD, 0, 0, 0, DEFAULT_CHARSET, OUT_DEFAULT_PRECIS,
                             CLIP_DEFAULT_PRECIS, CLEARTYPE_QUALITY, DEFAULT_PITCH, L"Segoe UI");
    font_sub = CreateFontW(-S(13), 0, 0, 0, FW_NORMAL, 0, 0, 0, DEFAULT_CHARSET, OUT_DEFAULT_PRECIS,
                           CLIP_DEFAULT_PRECIS, CLEARTYPE_QUALITY, DEFAULT_PITCH, L"Segoe UI");
    splash_icon = (HICON)LoadImageW(inst, MAKEINTRESOURCEW(1), IMAGE_ICON, S(32), S(32), LR_DEFAULTCOLOR);
    ShowWindow(splash, SW_SHOWNORMAL);
    UpdateWindow(splash);
    SetForegroundWindow(splash);
    SetTimer(splash, 1, 16, NULL);
}

/* Keep the splash drawn and answering for `ms`, or until the server is up (with up != 0). */
static int pump(DWORD ms, int up)
{
    DWORD start = GetTickCount();
    while (GetTickCount() - start < ms) {
        if (up && server_up())
            return 1;
        if (up && server_quit())
            return 0;
        DWORD left = ms - (GetTickCount() - start);
        MsgWaitForMultipleObjects(0, NULL, FALSE, left < 250 ? left : 250, QS_ALLINPUT);
        MSG m;
        while (PeekMessageW(&m, NULL, 0, 0, PM_REMOVE)) {
            TranslateMessage(&m);
            DispatchMessageW(&m);
        }
    }
    return up ? server_up() : 1;
}

static void splash_close(void)
{
    if (!splash)
        return;
    KillTimer(splash, 1);
    DestroyWindow(splash);
    splash = NULL;
    if (font_title) DeleteObject(font_title);
    if (font_sub) DeleteObject(font_sub);
    if (splash_icon) DestroyIcon(splash_icon);
}

/* ---------------------------------------------------------------- the window
 * A plain Windows window, nothing custom: what the server's /launcher API does (play here or on a
 * friend's server, host for friends, logs, quit), the mod menu (the server's /tweak page, in
 * Edge as an app) and the source on GitHub. It asks the server for its state every second, which
 * also tells the server the window is open (it stops itself once it's gone and Limbus is closed). */
static const wchar_t *GITHUB = L"https://github.com/paladiga484";
static const wchar_t *TWEAK = L"http://127.0.0.1:3000/tweak";
enum { ID_STATUS = 100, ID_PLAY, ID_JOINPLAY, ID_CANCEL, ID_HOST, ID_TOKEN, ID_SAVE, ID_TEST, ID_HOSTING,
       ID_HOSTADDR, ID_MODMENU, ID_LOGS, ID_GITHUB, ID_QUIT, ID_MANAGER, ID_TIMER = 1 };
static HWND win, status_label, host_label;
static void open_console_menu(void);
static HFONT ui_font;

/* One request to the server: method, path, JSON body (or NULL). The response body (UTF-8, NUL
 * ended) goes to out; returns the HTTP status, or 0 when the server didn't answer. */
static int http_wait(const char *method, const char *path, const char *body, char *out, int cap, DWORD ms)
{
    out[0] = 0;
    SOCKET s = socket(AF_INET, SOCK_STREAM, IPPROTO_TCP);
    if (s == INVALID_SOCKET)
        return 0;
    setsockopt(s, SOL_SOCKET, SO_RCVTIMEO, (const char *)&ms, sizeof ms);
    struct sockaddr_in a;
    ZeroMemory(&a, sizeof a);
    a.sin_family = AF_INET;
    a.sin_port = htons(PORT);
    a.sin_addr.s_addr = htonl(INADDR_LOOPBACK);
    if (connect(s, (struct sockaddr *)&a, sizeof a) != 0) {
        closesocket(s);
        return 0;
    }
    char req[4096];
    int n = snprintf(req, sizeof req,
                     "%s %s HTTP/1.1\r\nHost: 127.0.0.1:%d\r\nX-Mirror: 1\r\nConnection: close\r\n"
                     "Content-Type: application/json\r\nContent-Length: %d\r\n\r\n%s",
                     method, path, PORT, body ? (int)strlen(body) : 0, body ? body : "");
    if (n <= 0 || n >= (int)sizeof req || send(s, req, n, 0) != n) {
        closesocket(s);
        return 0;
    }
    static char resp[16384];
    int got = 0, r;
    while (got < (int)sizeof resp - 1 && (r = recv(s, resp + got, sizeof resp - 1 - got, 0)) > 0)
        got += r;
    closesocket(s);
    resp[got] = 0;
    int code = 0;
    if (sscanf(resp, "HTTP/1.%*d %d", &code) != 1)
        return 0;
    char *b = strstr(resp, "\r\n\r\n");
    if (b) {
        b += 4;
        if (strstr(resp, "chunked")) {                     /* one chunk is all these answers need */
            char *nl = strstr(b, "\r\n");
            if (nl) b = nl + 2;
        }
        snprintf(out, cap, "%s", b);
    }
    return code;
}

static int http(const char *method, const char *path, const char *body, char *out, int cap)
{
    return http_wait(method, path, body, out, cap, 3000);
}

/* A JSON string value: the text after "key":" up to its closing quote, unescaped simply. */
static void json_str(const char *json, const char *key, char *out, int cap)
{
    char pat[64];
    snprintf(pat, sizeof pat, "\"%s\":\"", key);
    const char *p = strstr(json, pat);
    int i = 0;
    if (p)
        for (p += strlen(pat); *p && *p != '"' && i < cap - 1; p++) {
            if (*p == '\\' && p[1]) p++;
            out[i++] = *p;
        }
    out[i] = 0;
}

static void set_utf8(HWND w, const char *text)
{
    wchar_t wide[1024];
    if (MultiByteToWideChar(CP_UTF8, 0, text, -1, wide, 1024) > 0)
        SetWindowTextW(w, wide);
}

/* A field's text as a JSON string's contents (UTF-8, quotes and backslashes escaped). */
static void field_json(int id, char *out, int cap)
{
    wchar_t wide[512];
    char utf8[1024];
    GetDlgItemTextW(win, id, wide, 512);
    WideCharToMultiByte(CP_UTF8, 0, wide, -1, utf8, sizeof utf8, NULL, NULL);
    int j = 0;
    for (char *p = utf8; *p && j < cap - 2; p++) {
        if ((unsigned char)*p < 0x20) continue;
        if (*p == '"' || *p == '\\') out[j++] = '\\';
        out[j++] = *p;
    }
    out[j] = 0;
}

/* After a request: its error, if the server gave one, in a message box. */
static void report(int code, const char *body, const wchar_t *what)
{
    if (code >= 200 && code < 300)
        return;
    char err[512];
    json_str(body, "error", err, sizeof err);
    wchar_t text[700];
    if (!code)
        _snwprintf(text, 700, L"%ls: the server isn't answering.", what);
    else if (!err[0] || MultiByteToWideChar(CP_UTF8, 0, err, -1, text, 700) <= 0)
        _snwprintf(text, 700, L"%ls didn't work (HTTP %d).", what, code);
    MessageBoxW(win, text, TITLE, MB_OK | MB_ICONWARNING);
}

static void refresh(void)
{
    static char body[16384];
    int code = http("GET", "/launcher/api/state", NULL, body, sizeof body);
    char line[1024], msg[512], addr[128];
    if (!code) {
        SetWindowTextW(status_label, L"The server stopped. Close this window and open the app again.");
        return;
    }
    json_str(body, "message", msg, sizeof msg);
    int game = strstr(body, "\"running\":true") != NULL;
    static int was_in;                         /* once you're in the game: the menu, by itself */
    int in = game && strstr(body, "\"phase\":\"attached\"") != NULL;
    if (in && !was_in)
        open_console_menu();
    was_in = in;
    snprintf(line, sizeof line, "Server: running     Limbus: %s%s%s", game ? "running" : "closed",
             msg[0] ? "     " : "", msg);
    set_utf8(status_label, line);
    json_str(body, "address", addr, sizeof addr);
    char hl[200];
    snprintf(hl, sizeof hl, "%s", addr[0] ? addr : "(this computer only)");
    set_utf8(host_label, hl);
    CheckDlgButton(win, ID_HOSTING, addr[0] ? BST_CHECKED : BST_UNCHECKED);
}

static void load_join(void)
{
    static char body[16384];
    char host[256];
    if (http("GET", "/launcher/api/state", NULL, body, sizeof body)) {
        const char *j = strstr(body, "\"join\":");
        json_str(j ? j : "", "host", host, sizeof host);
        set_utf8(GetDlgItem(win, ID_HOST), host);
        if (j && strstr(j, "\"tokenSet\":true"))
            SendDlgItemMessageW(win, ID_TOKEN, EM_SETCUEBANNER, TRUE, (LPARAM)L"(saved - type to replace)");
    }
}

static void open_mod_menu(void)
{
    wchar_t edge[MAX_PATH], local[MAX_PATH], args[2048];
    DWORD n = GetEnvironmentVariableW(L"LOCALAPPDATA", local, MAX_PATH);
    if (n > 0 && n < MAX_PATH && find_edge(edge, MAX_PATH)
        && _snwprintf(args, 2048,
               L"--app=%ls --user-data-dir=\"%ls\\LimbusPrivateServer\\window\" --window-size=1200,820 "
               L"--no-first-run --no-default-browser-check --disable-sync --disable-background-networking "
               L"--disable-breakpad --disable-component-update --disable-domain-reliability "
               L"--disable-client-side-phishing-detection "
               L"--disable-features=msEdgeEnableSmartScreen,EdgeCollections,EdgeDiscoverFeature",
               TWEAK, local) > 0
        && (INT_PTR)ShellExecuteW(NULL, L"open", edge, args, NULL, SW_SHOWNORMAL) > 32)
        return;
    ShellExecuteW(NULL, L"open", TWEAK, NULL, NULL, SW_SHOWNORMAL);
}

static void command(int id)
{
    static char body[16384];
    char json[1200], host[512], token[512];
    switch (id) {
    case ID_PLAY:
        report(http("POST", "/launcher/api/play", "{\"mode\":\"local\"}", body, sizeof body), body, L"Play");
        break;
    case ID_JOINPLAY:
        report(http("POST", "/launcher/api/play", "{\"mode\":\"join\"}", body, sizeof body), body, L"Play on a friend's server");
        break;
    case ID_CANCEL:
        http("POST", "/launcher/api/cancel", NULL, body, sizeof body);
        break;
    case ID_SAVE:
    case ID_TEST:
        field_json(ID_HOST, host, sizeof host);
        field_json(ID_TOKEN, token, sizeof token);
        if (id == ID_TEST) {
            snprintf(json, sizeof json, "{\"host\":\"%s\"}", host);
            int code = http("POST", "/launcher/api/join/test", json, body, sizeof body);
            char err[512];
            json_str(body, "error", err, sizeof err);
            wchar_t text[700];
            if (code == 200 && strstr(body, "\"ok\":true"))
                MessageBoxW(win, L"Your friend's server answers.", TITLE, MB_OK | MB_ICONINFORMATION);
            else {
                if (!err[0] || MultiByteToWideChar(CP_UTF8, 0, err, -1, text, 700) <= 0)
                    wcscpy(text, L"Your friend's server didn't answer.");
                MessageBoxW(win, text, TITLE, MB_OK | MB_ICONWARNING);
            }
            break;
        }
        snprintf(json, sizeof json, token[0] ? "{\"host\":\"%s\",\"token\":\"%s\"}" : "{\"host\":\"%s\"}", host, token);
        int code = http("POST", "/launcher/api/join", json, body, sizeof body);
        report(code, body, L"Saving your friend's server");
        if (code == 200) {
            SetDlgItemTextW(win, ID_TOKEN, L"");
            load_join();
        }
        break;
    case ID_HOSTING: {
        int on = IsDlgButtonChecked(win, ID_HOSTING) == BST_CHECKED;
        snprintf(json, sizeof json, "{\"on\":%s}", on ? "true" : "false");
        report(http("POST", "/launcher/api/hosting", json, body, sizeof body), body, L"Hosting");
        break;
    }
    case ID_MODMENU:
        open_mod_menu();
        break;
    case ID_LOGS:
        report(http("POST", "/launcher/api/logs", NULL, body, sizeof body), body, L"Opening the logs");
        break;
    case ID_GITHUB:
        ShellExecuteW(NULL, L"open", GITHUB, NULL, NULL, SW_SHOWNORMAL);
        break;
    case ID_MANAGER:
        open_console_menu();
        break;
    case ID_QUIT: {
        int code = http("POST", "/launcher/api/quit", NULL, body, sizeof body);
        report(code, body, L"Stopping the server");
        if (code == 200)
            DestroyWindow(win);
        break;
    }
    }
    refresh();
}

static HWND control(const wchar_t *cls, const wchar_t *text, DWORD style, int x, int y, int w, int h, int id)
{
    HWND c = CreateWindowExW(wcscmp(cls, L"EDIT") == 0 ? WS_EX_CLIENTEDGE : 0, cls, text,
                             WS_CHILD | WS_VISIBLE | style, x, y, w, h, win, (HMENU)(INT_PTR)id, NULL, NULL);
    SendMessageW(c, WM_SETFONT, (WPARAM)ui_font, TRUE);
    return c;
}

static void build(void)
{
    const DWORD B = WS_TABSTOP | BS_PUSHBUTTON, G = BS_GROUPBOX;
    status_label = control(L"STATIC", L"", SS_LEFT, 12, 10, 496, 20, ID_STATUS);
    control(L"BUTTON", L"Play", G, 10, 36, 500, 60, 0);
    control(L"BUTTON", L"&Play", B | BS_DEFPUSHBUTTON, 22, 58, 140, 28, ID_PLAY);
    control(L"BUTTON", L"Play on a &friend's server", B, 172, 58, 190, 28, ID_JOINPLAY);
    control(L"BUTTON", L"&Cancel", B, 372, 58, 126, 28, ID_CANCEL);
    control(L"BUTTON", L"Friend's server", G, 10, 104, 500, 92, 0);
    control(L"STATIC", L"Address:", SS_LEFT, 22, 128, 70, 20, 0);
    control(L"EDIT", L"", WS_TABSTOP | ES_AUTOHSCROLL, 96, 124, 260, 24, ID_HOST);
    control(L"STATIC", L"Token:", SS_LEFT, 22, 160, 70, 20, 0);
    control(L"EDIT", L"", WS_TABSTOP | ES_AUTOHSCROLL | ES_PASSWORD, 96, 156, 260, 24, ID_TOKEN);
    control(L"BUTTON", L"&Save", B, 366, 123, 132, 26, ID_SAVE);
    control(L"BUTTON", L"&Test", B, 366, 155, 132, 26, ID_TEST);
    control(L"BUTTON", L"Hosting", G, 10, 204, 500, 56, 0);
    control(L"BUTTON", L"&Host for friends (over Tailscale)", WS_TABSTOP | BS_AUTOCHECKBOX, 22, 226, 230, 24, ID_HOSTING);
    host_label = control(L"STATIC", L"", SS_LEFT, 260, 230, 240, 20, ID_HOSTADDR);
    control(L"BUTTON", L"Mana&ger", B, 10, 272, 93, 30, ID_MANAGER);
    control(L"BUTTON", L"&Mod menu", B, 111, 272, 93, 30, ID_MODMENU);
    control(L"BUTTON", L"Open &logs", B, 212, 272, 93, 30, ID_LOGS);
    control(L"BUTTON", L"&GitHub", B, 313, 272, 93, 30, ID_GITHUB);
    control(L"BUTTON", L"Stop &server", B, 414, 272, 96, 30, ID_QUIT);
}

static LRESULT CALLBACK window_proc(HWND w, UINT msg, WPARAM wp, LPARAM lp)
{
    switch (msg) {
    case WM_COMMAND:
        if (HIWORD(wp) == BN_CLICKED && LOWORD(wp) >= ID_PLAY)
            command(LOWORD(wp));
        return 0;
    case WM_TIMER:
        refresh();
        return 0;
    case WM_CTLCOLORSTATIC:
        SetBkMode((HDC)wp, TRANSPARENT);
        return (LRESULT)GetSysColorBrush(COLOR_BTNFACE);
    case WM_DESTROY:
        KillTimer(w, ID_TIMER);
        PostQuitMessage(0);
        return 0;
    }
    return DefWindowProcW(w, msg, wp, lp);
}

static void native_window(HINSTANCE inst)
{
    NONCLIENTMETRICSW ncm;
    ncm.cbSize = sizeof ncm;
    ui_font = SystemParametersInfoW(SPI_GETNONCLIENTMETRICS, sizeof ncm, &ncm, 0)
                  ? CreateFontIndirectW(&ncm.lfMessageFont) : (HFONT)GetStockObject(DEFAULT_GUI_FONT);
    WNDCLASSW wc;
    ZeroMemory(&wc, sizeof wc);
    wc.lpfnWndProc = window_proc;
    wc.hInstance = inst;
    wc.hIcon = LoadIconW(inst, MAKEINTRESOURCEW(1));
    wc.hCursor = LoadCursorW(NULL, IDC_ARROW);
    wc.hbrBackground = (HBRUSH)(COLOR_BTNFACE + 1);
    wc.lpszClassName = L"LimbusPrivateServerWindow";
    RegisterClassW(&wc);
    RECT r = {0, 0, 520, 314};
    DWORD style = WS_OVERLAPPED | WS_CAPTION | WS_SYSMENU | WS_MINIMIZEBOX;
    AdjustWindowRect(&r, style, FALSE);
    win = CreateWindowExW(0, wc.lpszClassName, TITLE, style, CW_USEDEFAULT, CW_USEDEFAULT,
                          r.right - r.left, r.bottom - r.top, NULL, NULL, inst, NULL);
    if (!win) {
        open_mod_menu();
        return;
    }
    build();
    load_join();
    refresh();
    SetTimer(win, ID_TIMER, 1000, NULL);
    ShowWindow(win, SW_SHOWNORMAL);
    SetForegroundWindow(win);
    if (splash) splash_close();
    MSG m;
    while (GetMessageW(&m, NULL, 0, 0) > 0)
        if (!IsDialogMessageW(win, &m)) {
            TranslateMessage(&m);
            DispatchMessageW(&m);
        }
}

/* ---------------------------------------------------------------- the console menu (--menu)
 * The same numbered menu as `limbus` on Linux, in a console window of its own: the logo in a
 * crimson gradient, a status line, bracketed columns, a prompt. It drives the server's /launcher
 * API as the window does. The window opens it by itself once the patch is attached; its Manager
 * button opens it any time. Updates: U, or the news on the status line. */

#ifndef ENABLE_VIRTUAL_TERMINAL_PROCESSING
#define ENABLE_VIRTUAL_TERMINAL_PROCESSING 0x0004
#endif
static const wchar_t *MENU_MUTEX = L"Local\\LimbusPrivateServerMenu";
static HANDLE con_out, con_in;
static int con_tty, con_vt;

static void out(const char *utf8)
{
    DWORD wrote;
    if (!con_tty) {
        WriteFile(con_out, utf8, (DWORD)strlen(utf8), &wrote, NULL);
        return;
    }
    int n = MultiByteToWideChar(CP_UTF8, 0, utf8, -1, NULL, 0);
    if (n <= 1)
        return;
    wchar_t *w = malloc(n * sizeof(wchar_t));
    if (!w)
        return;
    MultiByteToWideChar(CP_UTF8, 0, utf8, -1, w, n);
    WriteConsoleW(con_out, w, (DWORD)(n - 1), &wrote, NULL);
    free(w);
}

static void outf(const char *fmt, ...)
{
    char buf[8192];
    va_list ap;
    va_start(ap, fmt);
    vsnprintf(buf, sizeof buf, fmt, ap);
    va_end(ap);
    out(buf);
}

/* A line typed, without its line end, in UTF-8; -1 once the input has ended. */
static int in_line(char *buf, int cap)
{
    buf[0] = 0;
    if (con_tty) {
        wchar_t w[512];
        DWORD got = 0;
        if (!ReadConsoleW(con_in, w, 511, &got, NULL) || got == 0)
            return -1;
        while (got && (w[got - 1] == L'\n' || w[got - 1] == L'\r'))
            got--;
        w[got] = 0;
        WideCharToMultiByte(CP_UTF8, 0, w, -1, buf, cap, NULL, NULL);
    } else {
        int i = 0;
        char c;
        DWORD got;
        int any = 0;
        while (ReadFile(con_in, &c, 1, &got, NULL) && got == 1) {
            any = 1;
            if (c == '\n')
                break;
            if (c != '\r' && i < cap - 1)
                buf[i++] = c;
        }
        buf[i] = 0;
        if (!any)
            return -1;
    }
    char *p = buf, *e;                              /* trimmed */
    while (*p == ' ' || *p == '\t') p++;
    memmove(buf, p, strlen(p) + 1);
    for (e = buf + strlen(buf); e > buf && (e[-1] == ' ' || e[-1] == '\t'); e--) e[-1] = 0;
    return (int)strlen(buf);
}

/* A wide string (a path, a file name) as UTF-8, for the console: 4 at a time. */
static const char *u8(const wchar_t *w)
{
    static char buf[4][MAX_PATH * 3];
    static int k;
    k = (k + 1) % 4;
    if (WideCharToMultiByte(CP_UTF8, 0, w, -1, buf[k], sizeof buf[k], NULL, NULL) <= 0)
        buf[k][0] = 0;
    return buf[k];
}

/* Colours, when the console takes escape codes (Windows 10 and later); 8 at a time per line. */
static const char *fg(int r, int g, int b)
{
    static char buf[8][24];
    static int k;
    if (!con_vt)
        return "";
    k = (k + 1) % 8;
    snprintf(buf[k], sizeof buf[k], "\x1b[38;2;%d;%d;%dm", r, g, b);
    return buf[k];
}
#define RED fg(220, 20, 40)
#define WHITE fg(240, 240, 240)
#define GREY fg(130, 130, 130)
#define GREEN fg(80, 200, 120)
#define RST (con_vt ? "\x1b[0m" : "")
#define BOLD (con_vt ? "\x1b[1m" : "")

static int con_width(void)
{
    CONSOLE_SCREEN_BUFFER_INFO info;
    if (con_tty && GetConsoleScreenBufferInfo(con_out, &info))
        return info.srWindow.Right - info.srWindow.Left + 1;
    return 100;
}

static void con_clear(void)
{
    if (con_vt)
        out("\x1b[2J\x1b[3J\x1b[H");
}

static int console_open(void)
{
    HANDLE o = GetStdHandle(STD_OUTPUT_HANDLE), i = GetStdHandle(STD_INPUT_HANDLE);
    DWORD t = (o && o != INVALID_HANDLE_VALUE) ? GetFileType(o) : FILE_TYPE_UNKNOWN;
    if (t == FILE_TYPE_PIPE || t == FILE_TYPE_DISK) {       /* redirected (the tests): plain text */
        con_out = o;
        con_in = i;
        return 1;
    }
    AllocConsole();                                         /* a window of its own */
    con_out = CreateFileW(L"CONOUT$", GENERIC_READ | GENERIC_WRITE, FILE_SHARE_READ | FILE_SHARE_WRITE, NULL,
                          OPEN_EXISTING, 0, NULL);
    con_in = CreateFileW(L"CONIN$", GENERIC_READ | GENERIC_WRITE, FILE_SHARE_READ | FILE_SHARE_WRITE, NULL,
                         OPEN_EXISTING, 0, NULL);
    if (con_out == INVALID_HANDLE_VALUE || con_in == INVALID_HANDLE_VALUE)
        return 0;
    con_tty = 1;
    DWORD mode;
    con_vt = GetConsoleMode(con_out, &mode)
             && SetConsoleMode(con_out, mode | ENABLE_PROCESSED_OUTPUT | ENABLE_VIRTUAL_TERMINAL_PROCESSING);
    SetConsoleOutputCP(CP_UTF8);
    SetConsoleTitleW(L"Limbus Private Server - menu");
    return 1;
}

static const char *LOGO[] = {
    "██╗     ██╗███╗   ███╗██████╗ ██╗   ██╗███████╗",
    "██║     ██║████╗ ████║██╔══██╗██║   ██║██╔════╝",
    "██║     ██║██╔████╔██║██████╔╝██║   ██║███████╗",
    "██║     ██║██║╚██╔╝██║██╔══██╗██║   ██║╚════██║",
    "███████╗██║██║ ╚═╝ ██║██████╔╝╚██████╔╝███████║",
    "╚══════╝╚═╝╚═╝     ╚═╝╚═════╝  ╚═════╝ ╚══════╝"};
static const char *LOGO_ASCII[] = {
    " _     ___ __  __ ____  _   _ ____",
    "| |   |_ _|  \\/  | __ )| | | / ___|",
    "| |    | || |\\/| |  _ \\| | | \\___ \\",
    "| |___ | || |  | | |_) | |_| |___) |",
    "|_____|___|_|  |_|____/ \\___/|____/"};

/* UTF-8 characters in a string (what a terminal cell holds) */
static int cells(const char *s)
{
    int n = 0;
    for (; *s; s++)
        if (((unsigned char)*s & 0xC0) != 0x80)
            n++;
    return n;
}

/* Visible width of a line with escape codes in it. */
static int visible(const char *s)
{
    int n = 0;
    while (*s) {
        if (*s == '\x1b') {
            while (*s && *s != 'm') s++;
            if (*s) s++;
            continue;
        }
        if (((unsigned char)*s & 0xC0) != 0x80)
            n++;
        s++;
    }
    return n;
}

static char release_str[64] = "";

static void con_header(void)
{
    const char **logo = con_tty ? LOGO : LOGO_ASCII;
    int rows = 6, width = con_width(), wide = 0;
    if (!con_tty)
        rows = 5;
    for (int r = 0; r < rows; r++)
        if (cells(logo[r]) > wide) wide = cells(logo[r]);
    int pad = width > wide ? (width - wide) / 2 : 0;
    for (int r = 0; r < rows; r++) {
        char line[2048];
        int at = snprintf(line, sizeof line, "%*s", pad, "");
        int len = cells(logo[r]), i = 0;
        for (const char *p = logo[r]; *p && at < (int)sizeof line - 32; i++) {
            /* a crimson gradient, dark at the top left to bright at the bottom right */
            int t100 = (i * 100 / (len ? len : 1) + r * 100 / rows) / 2;
            at += snprintf(line + at, sizeof line - at, "%s", fg(110 + 145 * t100 / 100, 10 + 30 * t100 / 100, 25 + 30 * t100 / 100));
            do line[at++] = *p++; while (*p && ((unsigned char)*p & 0xC0) == 0x80);
        }
        snprintf(line + at, sizeof line - at, "%s\n", RST);
        out(line);
    }
    char sub[128];
    snprintf(sub, sizeof sub, con_tty ? "private server  \xc2\xb7  v%s" : "private server  -  v%s",
             release_str[0] ? release_str : "?");
    outf("%*s%s%s%s\n", width > cells(sub) ? (width - cells(sub)) / 2 : 0, "", GREY, sub, RST);
    const char *gh = "github.com/paladiga484";
    outf("%*s%s%s%s\n\n", width > (int)strlen(gh) ? (width - (int)strlen(gh)) / 2 : 0, "", RED, gh, RST);
}

static void con_pause(void)
{
    char b[16];
    outf("\n%sEnter to go back%s ", GREY, RST);
    in_line(b, sizeof b);
}

/* After a request: what went wrong, if it did. 1 when it worked. */
static int con_report(int code, const char *body, const char *what)
{
    if (code >= 200 && code < 300)
        return 1;
    char err[512];
    json_str(body, "error", err, sizeof err);
    if (!code)
        outf("\n  %s%s: the server isn't answering. 07 opens the app, which starts it.%s\n", RED, what, RST);
    else
        outf("\n  %s%s%s%s\n", RED, err[0] ? err : what, err[0] ? "" : " didn't work", RST);
    return 0;
}

/* A string typed for JSON: quotes and backslashes escaped, control characters dropped. */
static void json_escape(const char *in, char *out_, int cap)
{
    int j = 0;
    for (const char *p = in; *p && j < cap - 2; p++) {
        if ((unsigned char)*p < 0x20) continue;
        if (*p == '"' || *p == '\\') out_[j++] = '\\';
        out_[j++] = *p;
    }
    out_[j] = 0;
}

/* ---- updates: the server downloads, checks and unpacks; the swap happens here ---- */

/* The player's own files stay: their save, keys and client settings (a friend's server, token). */
static int keep_theirs(const wchar_t *rel)
{
    size_t n = wcslen(rel);
    return !_wcsicmp(rel, L"client\\myosotis.ini") || !_wcsicmp(rel, L"join\\myosotis.ini")
           || (n > 4 && !_wcsicmp(rel + n - 4, L".key")) || !_wcsnicmp(rel, L"server\\myosotis.db", 18);
}

/* Copy src (ending in a backslash) over dst (the same): a file in use (this very program) is
 * moved aside to <name>.old first, which Windows allows. 0 when something couldn't be written. */
static int copy_tree(const wchar_t *src, const wchar_t *dst, const wchar_t *rel, int *count)
{
    wchar_t pat[MAX_PATH];
    if (_snwprintf(pat, MAX_PATH, L"%ls*", src) < 0)
        return 0;
    WIN32_FIND_DATAW fd;
    HANDLE h = FindFirstFileW(pat, &fd);
    if (h == INVALID_HANDLE_VALUE)
        return 0;
    CreateDirectoryW(dst, NULL);
    int ok = 1;
    do {
        if (!wcscmp(fd.cFileName, L".") || !wcscmp(fd.cFileName, L".."))
            continue;
        int dir = (fd.dwFileAttributes & FILE_ATTRIBUTE_DIRECTORY) != 0;
        wchar_t s[MAX_PATH], d[MAX_PATH], r[MAX_PATH], old[MAX_PATH];
        if (_snwprintf(s, MAX_PATH, dir ? L"%ls%ls\\" : L"%ls%ls", src, fd.cFileName) < 0
            || _snwprintf(d, MAX_PATH, dir ? L"%ls%ls\\" : L"%ls%ls", dst, fd.cFileName) < 0
            || _snwprintf(r, MAX_PATH, rel[0] ? L"%ls\\%ls" : L"%ls%ls", rel, fd.cFileName) < 0) {
            ok = 0;
            break;
        }
        if (dir) {
            if (!copy_tree(s, d, r, count)) { ok = 0; break; }
            continue;
        }
        if (keep_theirs(r) && exists(d))
            continue;
        if (!CopyFileW(s, d, FALSE)) {
            if (_snwprintf(old, MAX_PATH, L"%ls.old", d) < 0) { ok = 0; break; }
            DeleteFileW(old);
            if (!MoveFileExW(d, old, MOVEFILE_REPLACE_EXISTING) || !CopyFileW(s, d, FALSE)) {
                outf("  %scouldn't write %s%s\n", RED, u8(r), RST);
                ok = 0;
                break;
            }
        }
        (*count)++;
    } while (FindNextFileW(h, &fd));
    FindClose(h);
    return ok;
}

static void remove_tree(const wchar_t *dir)
{
    wchar_t from[MAX_PATH + 2];
    ZeroMemory(from, sizeof from);
    if (_snwprintf(from, MAX_PATH, L"%ls", dir) < 0)
        return;
    SHFILEOPSTRUCTW op;
    ZeroMemory(&op, sizeof op);
    op.wFunc = FO_DELETE;
    op.pFrom = from;                                        /* double-NUL ended */
    op.fFlags = FOF_NO_UI;
    SHFileOperationW(&op);
}

/* update\staged\LimbusPrivateServer over this folder. The number of files, or -1. */
static int apply_staged(void)
{
    wchar_t src[MAX_PATH], top[MAX_PATH];
    if (_snwprintf(top, MAX_PATH, L"%lsupdate", here) < 0
        || _snwprintf(src, MAX_PATH, L"%lsupdate\\staged\\LimbusPrivateServer\\", here) < 0)
        return -1;
    wchar_t probe[MAX_PATH];
    if (_snwprintf(probe, MAX_PATH, L"%lsserver\\Server.exe", src) < 0 || !exists(probe))
        return -1;
    int count = 0;
    if (!copy_tree(src, here, L"", &count))
        return -1;
    remove_tree(top);
    return count;
}

static void start_app(void)
{
    wchar_t exe[MAX_PATH], cmd[MAX_PATH + 2];
    if (GetModuleFileNameW(NULL, exe, MAX_PATH) == 0 || _snwprintf(cmd, MAX_PATH + 2, L"\"%ls\"", exe) < 0)
        return;
    STARTUPINFOW si;
    PROCESS_INFORMATION pi;
    ZeroMemory(&si, sizeof si);
    si.cb = sizeof si;
    if (CreateProcessW(exe, cmd, NULL, NULL, FALSE, 0, NULL, here, &si, &pi)) {
        CloseHandle(pi.hThread);
        CloseHandle(pi.hProcess);
    }
}

/* The download is staged: stop the server, close the window, copy it in, start the new app. */
static int restart_into_update(void)
{
    static char body[16384];
    if (!con_report(http("POST", "/launcher/api/quit", NULL, body, sizeof body), body, "Stopping the server"))
        return 0;
    outf("  %sstopping the server...%s\n", GREY, RST);
    if (!wait_for(0, 30)) {
        outf("  %sThe server didn't stop - nothing was changed.%s\n", RED, RST);
        return 0;
    }
    HWND w = FindWindowW(L"LimbusPrivateServerWindow", NULL);
    if (w)
        PostMessageW(w, WM_CLOSE, 0, 0);
    Sleep(800);
    int n = apply_staged();
    if (n < 0) {
        outf("  %sThe update couldn't be copied in (see above). Start the app again: it still works.%s\n", RED, RST);
        return 0;
    }
    outf("  %s%d files updated; your save, keys and friend settings stayed.%s\n", GREEN, n, RST);
    start_app();
    return 1;
}

static void progress_bar(int pct, const char *what)
{
    int w = 30, fill = pct * w / 100;
    char bar[256];
    int at = 0;
    for (int i = 0; i < w; i++)
        at += snprintf(bar + at, sizeof bar - at, "%s", i < fill ? (con_tty ? "\xe2\x96\x88" : "#") : (con_tty ? "\xe2\x96\x91" : "."));
    if (con_tty)
        outf("\r  %s[%s%s%s]%s %3d%%  %s%-40.40s%s", RED, GREEN, bar, RED, RST, pct, GREY, what, RST);
}

static void con_update(void)
{
    static char body[4096];
    char current[64], latest[64], when[64], err[300], phase[32], msg[300], b[64];
    for (;;) {
        con_clear();
        con_header();
        outf("  %s[%s%s%sUpdate%s%s]%s\n\n", RED, RST, BOLD, WHITE, RST, RED, RST);
        if (!http("GET", "/launcher/api/update", NULL, body, sizeof body)) {
            outf("  %sThe server isn't running: 07 opens the app, which starts it.%s\n", RED, RST);
            con_pause();
            return;
        }
        json_str(body, "current", current, sizeof current);
        json_str(body, "latest", latest, sizeof latest);
        json_str(body, "checked", when, sizeof when);
        json_str(body, "error", err, sizeof err);
        int newer = strstr(body, "\"newer\":true") != NULL, autocheck = strstr(body, "\"auto\":false") == NULL;
        if (!current[0]) {
            outf("  This copy doesn't know its own release, so it can't update itself.\n");
            con_pause();
            return;
        }
        outf("  this install   %s%s%s\n", WHITE, current, RST);
        outf("  newest         %s%s%s   %schecked %s%s\n", WHITE, latest[0] ? latest : "-", RST, GREY, when[0] && !err[0] ? when : "-", RST);
        if (err[0])
            outf("  %slast check: %s%s\n", RED, err, RST);
        out("\n");
        if (newer)
            outf("  %s[%s1%s]%s %sDownload and install %s%s  %s(your save and settings stay)%s\n", RED, WHITE, RED, RST, GREEN, latest, RST, GREY, RST);
        else
            outf("  %s[1] (nothing newer)%s\n", GREY, RST);
        outf("  %s[%s2%s]%s Check now\n", RED, WHITE, RED, RST);
        outf("  %s[%s3%s]%s Check by itself when the menu opens: %s%s%s  %s(asks GitHub, twice a day at most)%s\n",
             RED, WHITE, RED, RST, WHITE, autocheck ? "on" : "off", RST, GREY, RST);
        outf("\n  %sEnter: back%s  %s>%s ", GREY, RST, RED, RST);
        if (in_line(b, sizeof b) <= 0)
            return;
        if (!strcmp(b, "2")) {
            outf("\n  %sasking GitHub...%s\n", GREY, RST);
            http_wait("POST", "/launcher/api/update/check", "{\"force\":true}", body, sizeof body, 20000);
        } else if (!strcmp(b, "3")) {
            http("POST", "/launcher/api/update/auto", autocheck ? "{\"on\":false}" : "{\"on\":true}", body, sizeof body);
        } else if (!strcmp(b, "1") && newer) {
            out("\n");
            if (!con_report(http("POST", "/launcher/api/update", NULL, body, sizeof body), body, "The update")) {
                con_pause();
                continue;
            }
            phase[0] = msg[0] = 0;
            for (int i = 0; i < 20 * 60 * 4; i++) {             /* up to 20 minutes */
                if (!http("GET", "/launcher/api/update", NULL, body, sizeof body))
                    break;
                json_str(body, "phase", phase, sizeof phase);
                json_str(body, "message", msg, sizeof msg);
                const char *pc = strstr(body, "\"percent\":");
                progress_bar(pc ? atoi(pc + 10) : 0, msg);
                if (!strcmp(phase, "ready") || !strcmp(phase, "failed"))
                    break;
                Sleep(250);
            }
            out("\n");
            if (strcmp(phase, "ready")) {
                outf("  %s%s%s\n", RED, msg[0] ? msg : "The update stopped.", RST);
                con_pause();
                continue;
            }
            outf("  %schecksum ok%s - %s\n\n  Restart into it now? Limbus must be closed. [y/n] ", GREEN, RST, msg);
            if (in_line(b, sizeof b) >= 0 && (b[0] == 'y' || b[0] == 'Y') && restart_into_update()) {
                outf("\n  %sUpdated. The app is starting again; this menu closes.%s\n", GREEN, RST);
                Sleep(2000);
                ExitProcess(0);
            }
            con_pause();
        } else if (!b[0]) {
            return;
        }
    }
}

/* ---- the front page ---- */

enum { A_PLAY = 1, A_JOIN, A_CANCEL, A_STOP, A_MODMENU, A_LOGS, A_WINDOW, A_HOST, A_SETJOIN, A_TEST };
static const struct { const char *column, *num, *label; int act; } ITEMS[] = {
    {"Play", "01", "Play", A_PLAY},          {"Play", "02", "Friend's server", A_JOIN},
    {"Play", "03", "Cancel the start", A_CANCEL}, {"Play", "04", "Stop the server", A_STOP},
    {"Mods", "05", "Mod menu", A_MODMENU},   {"Mods", "06", "Open the logs", A_LOGS},
    {"Mods", "07", "The app window", A_WINDOW},
    {"Friends", "08", "Host for friends", A_HOST}, {"Friends", "09", "Set a friend's server", A_SETJOIN},
    {"Friends", "10", "Test it", A_TEST},
};
#define NITEMS ((int)(sizeof ITEMS / sizeof ITEMS[0]))

static void con_status(const char *state, int up, const char *update)
{
    const char *on = con_tty ? "\xe2\x97\x8f" : "[on]", *off = con_tty ? "\xe2\x97\x8b" : "[off]";
    int game = strstr(state, "\"running\":true") != NULL;
    char addr[128], latest[64];
    const char *h = strstr(state, "\"hosting\":");
    json_str(h ? h : "", "address", addr, sizeof addr);
    outf("  %s%s%s %sserver%s   %s%s%s %sgame%s   %shosting:%s %s",
         up ? GREEN : RED, up ? on : off, RST, WHITE, RST, game ? GREEN : RED, game ? on : off, RST, WHITE, RST,
         WHITE, RST, addr[0] ? addr : "off");
    json_str(update, "latest", latest, sizeof latest);
    if (strstr(update, "\"newer\":true"))
        outf("   %s%supdate %s - press U%s", GREEN, BOLD, latest, RST);
    out("\n");
    char msg[300];
    const char *p = strstr(state, "\"play\":");
    json_str(p ? p : "", "message", msg, sizeof msg);
    if (msg[0])
        outf("  %s%s%s\n", GREY, msg, RST);
    out("\n");
}

static void con_columns(void)
{
    const char *cols[] = {"Play", "Mods", "Friends"};
    int width = con_width(), per_row = width / 27;
    if (per_row < 1) per_row = 1;
    if (per_row > 3) per_row = 3;
    for (int start = 0; start < 3; start += per_row) {
        for (int line = 0; line < 5; line++) {
            char row[1024];
            int at = snprintf(row, sizeof row, "  ");
            for (int c = start; c < start + per_row && c < 3; c++) {
                char cell[256] = "";
                if (line == 0)
                    snprintf(cell, sizeof cell, "%s[%s%s%s%s%s]%s", RED, RST, BOLD, WHITE, cols[c], RED, RST);
                else {
                    int k = 0;
                    for (int i = 0; i < NITEMS; i++)
                        if (!strcmp(ITEMS[i].column, cols[c]) && ++k == line)
                            snprintf(cell, sizeof cell, "%s[%s%s%s]%s %s%s%s", RED, WHITE, ITEMS[i].num, RED, RST, WHITE, ITEMS[i].label, RST);
                }
                at += snprintf(row + at, sizeof row - at, "%s%*s", cell, 27 - visible(cell) > 0 ? 27 - visible(cell) : 1, "");
            }
            while (at > 0 && row[at - 1] == ' ') row[--at] = 0;
            outf("%s\n", row);
        }
        out("\n");
    }
    outf("  %s[%sU%s]%s Update   %s[%sG%s]%s GitHub   %s[%sH%s]%s Help   %s[%sQ%s]%s Quit\n",
         RED, WHITE, RED, RST, RED, WHITE, RED, RST, RED, WHITE, RED, RST, RED, WHITE, RED, RST);
}

/* Play's progress, from the server's state, until the patch is in or the start ends. */
static void con_follow(void)
{
    static char body[16384];
    char phase[32] = "", msg[300], last[300] = "";
    for (int i = 0; i < 6 * 60 * 4; i++) {
        if (!http("GET", "/launcher/api/state", NULL, body, sizeof body)) {
            outf("  %sthe server stopped%s\n", RED, RST);
            return;
        }
        const char *p = strstr(body, "\"play\":");
        json_str(p ? p : "", "phase", phase, sizeof phase);
        json_str(p ? p : "", "message", msg, sizeof msg);
        if (msg[0] && strcmp(msg, last)) {
            outf("  %s>>%s %s\n", RED, RST, msg);
            snprintf(last, sizeof last, "%s", msg);
        }
        if (!strcmp(phase, "attached")) {
            outf("\n  %sYou're in. The menu stays here while you play.%s\n", GREEN, RST);
            return;
        }
        if (!strcmp(phase, "failed") || (!strcmp(phase, "idle") && i > 8))
            return;
        Sleep(250);
    }
}

static void con_help(void)
{
    con_clear();
    con_header();
    outf("  %s%sHow it works%s\n"
         "  Type a number and Enter. Play starts Limbus through Steam and attaches the patch; once\n"
         "  it's in, this menu opens by itself (the window's Manager button opens it any time). The\n"
         "  mod menu (05) holds the server's settings while it runs. Hosting for friends works over\n"
         "  Tailscale only.\n\n"
         "  U checks GitHub for a newer release and installs it when you say so, after the server\n"
         "  has checked the download against its checksums. The menu also asks by itself when it\n"
         "  opens, twice a day at most; the Update page turns that off.\n", WHITE, BOLD, RST);
    con_pause();
}

static int console_menu(void)
{
    HANDLE mutex = CreateMutexW(NULL, FALSE, MENU_MUTEX);
    if (mutex && GetLastError() == ERROR_ALREADY_EXISTS)
        return 0;                                           /* one menu is enough */
    if (!console_open())
        return 1;
    static char state[16384], update[4096], body[16384];
    char b[512], host[512], token[512], json[1200], name[128];
    DWORD nlen = 128;
    if (!GetUserNameA(name, &nlen))
        snprintf(name, sizeof name, "you");
    /* the release, and the check when it's due (the server knows: it's off, or asked lately) */
    if (http("GET", "/launcher/api/update", NULL, update, sizeof update))
        json_str(update, "current", release_str, sizeof release_str);
    http_wait("POST", "/launcher/api/update/check", "{\"force\":false}", update, sizeof update, 15000);
    for (;;) {
        int up = http("GET", "/launcher/api/state", NULL, state, sizeof state) != 0;
        if (!up)
            state[0] = 0;
        if (!http("GET", "/launcher/api/update", NULL, update, sizeof update))
            update[0] = 0;
        if (!release_str[0])
            json_str(update, "current", release_str, sizeof release_str);
        con_clear();
        con_header();
        con_status(state, up, update);
        con_columns();
        if (con_tty)
            outf("\n%s\xe2\x94\x8c\xe2\x94\x80\xe2\x94\x80(%s%s@limbus%s)\xe2\x94\x80[%s%s%s]%s\n%s\xe2\x94\x94\xe2\x94\x80$%s ",
                 RED, WHITE, name, RED, WHITE, u8(here), RED, RST, RED, RST);
        else
            outf("\n+--(%s@limbus)-[%s]\n\\-$ ", name, u8(here));
        if (in_line(b, sizeof b) < 0)
            return 0;
        for (char *p = b; *p; p++) *p = (char)tolower((unsigned char)*p);
        if (!strcmp(b, "q") || !strcmp(b, "quit") || !strcmp(b, "exit"))
            return 0;
        if (!b[0])
            continue;
        if (!strcmp(b, "u")) { con_update(); continue; }
        if (!strcmp(b, "h")) { con_help(); continue; }
        if (!strcmp(b, "g")) { ShellExecuteW(NULL, L"open", GITHUB, NULL, NULL, SW_SHOWNORMAL); continue; }
        int act = 0;
        for (int i = 0; i < NITEMS; i++)
            if (atoi(b) > 0 && atoi(b) == atoi(ITEMS[i].num))
                act = ITEMS[i].act;
        if (!act) {
            outf("%sno such entry: %s%s\n", RED, b, RST);
            Sleep(800);
            continue;
        }
        out("\n");
        switch (act) {
        case A_PLAY:
        case A_JOIN:
            if (con_report(http("POST", "/launcher/api/play", act == A_PLAY ? "{\"mode\":\"local\"}" : "{\"mode\":\"join\"}",
                                body, sizeof body), body, "Play"))
                con_follow();
            break;
        case A_CANCEL:
            con_report(http("POST", "/launcher/api/cancel", NULL, body, sizeof body), body, "Cancel");
            break;
        case A_STOP:
            if (con_report(http("POST", "/launcher/api/quit", NULL, body, sizeof body), body, "Stopping the server"))
                outf("  the server is stopping. 07 starts it again.\n");
            break;
        case A_MODMENU:
            if (up) open_mod_menu();
            else outf("  %sThe mod menu opens while the server runs: 07 starts it.%s\n", RED, RST);
            break;
        case A_LOGS:
            con_report(http("POST", "/launcher/api/logs", NULL, body, sizeof body), body, "Opening the logs");
            break;
        case A_WINDOW: {
            HWND w = FindWindowW(L"LimbusPrivateServerWindow", NULL);
            if (w) {
                ShowWindow(w, SW_RESTORE);
                SetForegroundWindow(w);
            } else
                start_app();
            outf("  the app's window is %s.\n", w ? "in front" : "opening");
            break;
        }
        case A_HOST: {
            char addr[128];
            const char *h = strstr(state, "\"hosting\":");
            json_str(h ? h : "", "address", addr, sizeof addr);
            if (con_report(http("POST", "/launcher/api/hosting", addr[0] ? "{\"on\":false}" : "{\"on\":true}", body, sizeof body),
                           body, "Hosting"))
                outf("  hosting %s - the server restarts.\n", addr[0] ? "off" : "on");
            break;
        }
        case A_SETJOIN:
            outf("  Your friend's Tailscale address (like 100.64.0.7): ");
            if (in_line(b, sizeof b) <= 0) break;
            json_escape(b, host, sizeof host);
            outf("  Your token (Enter keeps the saved one): ");
            if (in_line(b, sizeof b) < 0) break;
            json_escape(b, token, sizeof token);
            snprintf(json, sizeof json, token[0] ? "{\"host\":\"%s\",\"token\":\"%s\"}" : "{\"host\":\"%s\"}", host, token);
            if (con_report(http("POST", "/launcher/api/join", json, body, sizeof body), body, "Saving your friend's server"))
                outf("  %ssaved.%s 02 plays on it.\n", GREEN, RST);
            break;
        case A_TEST: {
            const char *j = strstr(state, "\"join\":");
            json_str(j ? j : "", "host", b, sizeof b);
            json_escape(b, host, sizeof host);
            snprintf(json, sizeof json, "{\"host\":\"%s\"}", host);
            int code = http_wait("POST", "/launcher/api/join/test", json, body, sizeof body, 8000);
            if (code == 200 && strstr(body, "\"ok\":true"))
                outf("  %sYour friend's server answers.%s\n", GREEN, RST);
            else {
                char err[300];
                json_str(body, "error", err, sizeof err);
                outf("  %s%s%s\n", RED, err[0] ? err : "Your friend's server didn't answer.", RST);
            }
            break;
        }
        }
        con_pause();
    }
}

/* The window's way in: the menu in a console of its own, unless one is open already. */
static void open_console_menu(void)
{
    HANDLE m = OpenMutexW(SYNCHRONIZE, FALSE, MENU_MUTEX);
    if (m) {
        CloseHandle(m);
        return;
    }
    wchar_t exe[MAX_PATH], cmd[MAX_PATH + 16];
    if (GetModuleFileNameW(NULL, exe, MAX_PATH) == 0 || _snwprintf(cmd, MAX_PATH + 16, L"\"%ls\" --menu", exe) < 0)
        return;
    STARTUPINFOW si;
    PROCESS_INFORMATION pi;
    ZeroMemory(&si, sizeof si);
    si.cb = sizeof si;
    if (CreateProcessW(exe, cmd, NULL, NULL, FALSE, 0, NULL, here, &si, &pi)) {
        CloseHandle(pi.hThread);
        CloseHandle(pi.hProcess);
    }
}

int WINAPI wWinMain(HINSTANCE inst, HINSTANCE prev, PWSTR cmdline, int show)
{
    (void)prev; (void)show;
    WSADATA wsa;
    if (WSAStartup(MAKEWORD(2, 2), &wsa) != 0)
        return 1;
    DWORD n = GetModuleFileNameW(NULL, here, MAX_PATH);
    if (n == 0 || n >= MAX_PATH)
        return 1;
    wchar_t *slash = wcsrchr(here, L'\\');
    if (!slash)
        return 1;
    slash[1] = 0;

    if (wcsstr(cmdline, L"--menu")) {
        int rc = console_menu();
        WSACleanup();
        return rc;
    }
    wchar_t old_exe[MAX_PATH];                  /* this program as it was before an update */
    if (_snwprintf(old_exe, MAX_PATH, L"%lsLimbus Private Server.exe.old", here) > 0)
        DeleteFileW(old_exe);

    int restart = wcsstr(cmdline, L"--restart") != NULL;
    int headless = wcsstr(cmdline, L"--no-window") != NULL;

    if (restart && !wait_for(0, 30))
        return 1;                              /* the old server never stopped: leave it be */
    if (!server_up()) {
        switch (start_server()) {
        case -1:
            say(L"server\\Server.exe is missing.\n\nUnzip the whole folder somewhere normal "
                L"(Desktop, Documents) and open the app from there.");
            return 1;
        case 0:
            say(L"Windows wouldn't start the server (server\\Server.exe).\n\nYour antivirus may have "
                L"blocked it - README.txt explains why it can look suspicious.");
            return 1;
        }
        if (!restart && !headless)
            splash_open(inst);
        if (!(splash ? pump(90000, 1) : wait_for(1, 90))) {
            splash_close();
            if (game_data_error())
                return 1;
            say(L"The server didn't start.\n\nAnother program may be using port 3000, or the folder "
                L"was only partly unzipped. The server's log is in server\\logs.");
            return 1;
        }
    }
    if (!restart && !headless) {
        native_window(inst);                   /* until it's closed */
        splash_close();
    }
    WSACleanup();
    return 0;
}
