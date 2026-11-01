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
#include <wchar.h>
#include <stdio.h>
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
       ID_HOSTADDR, ID_MODMENU, ID_LOGS, ID_GITHUB, ID_QUIT, ID_TIMER = 1 };
static HWND win, status_label, host_label;
static HFONT ui_font;

/* One request to the server: method, path, JSON body (or NULL). The response body (UTF-8, NUL
 * ended) goes to out; returns the HTTP status, or 0 when the server didn't answer. */
static int http(const char *method, const char *path, const char *body, char *out, int cap)
{
    out[0] = 0;
    SOCKET s = socket(AF_INET, SOCK_STREAM, IPPROTO_TCP);
    if (s == INVALID_SOCKET)
        return 0;
    DWORD ms = 3000;
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
    control(L"BUTTON", L"&Mod menu", B, 10, 272, 118, 30, ID_MODMENU);
    control(L"BUTTON", L"Open &logs", B, 137, 272, 118, 30, ID_LOGS);
    control(L"BUTTON", L"&GitHub", B, 264, 272, 118, 30, ID_GITHUB);
    control(L"BUTTON", L"Stop &server", B, 391, 272, 119, 30, ID_QUIT);
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
