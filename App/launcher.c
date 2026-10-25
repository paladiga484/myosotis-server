/* Limbus Private Server - the Windows app.
 *
 * Starts the server out of sight (no console window), shows a small splash while it comes up,
 * and opens its window: Edge in app mode, pointed at http://127.0.0.1:3000/launcher, on a
 * profile of its own kept off the network. Everything else - playing, joining a friend,
 * hosting, quitting - happens in that window, and the server stops itself once the window is
 * closed and Limbus isn't running.
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
#include <wchar.h>

#define PORT 3000
static const wchar_t *TITLE = L"Limbus Private Server";
static const wchar_t *URL = L"http://127.0.0.1:3000/launcher";

static wchar_t here[MAX_PATH];                 /* this exe's folder, with a trailing backslash */

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

static int wait_for(int up, int seconds)
{
    for (int i = 0; i < seconds * 4; i++) {
        if (server_up() == up)
            return 1;
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
    CloseHandle(pi.hProcess);
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

/* ---- the splash: the app's icon, its name and a brass bar, while the server starts ---- */

static HWND splash;
static HFONT font_title, font_sub;
static HICON splash_icon;
static int splash_tick, dpi = 96;
#define S(x) MulDiv((x), dpi, 96)

static void fill(HDC dc, int l, int t, int r, int b, COLORREF c)
{
    RECT rc = {l, t, r, b};
    HBRUSH br = CreateSolidBrush(c);
    FillRect(dc, &rc, br);
    DeleteObject(br);
}

static void splash_draw(HDC dc, RECT rc)
{
    fill(dc, 0, 0, rc.right, rc.bottom, RGB(0x4b, 0x43, 0x37));                       /* outline */
    fill(dc, 1, 1, rc.right - 1, rc.bottom - 1, RGB(0x15, 0x12, 0x0e));               /* umber */
    if (splash_icon)
        DrawIconEx(dc, S(30), S(36), splash_icon, S(64), S(64), 0, NULL, DI_NORMAL);
    SetBkMode(dc, TRANSPARENT);
    SelectObject(dc, font_title);
    SetTextColor(dc, RGB(0xed, 0xe3, 0xd1));
    TextOutW(dc, S(114), S(44), TITLE, (int)wcslen(TITLE));
    SelectObject(dc, font_sub);
    SetTextColor(dc, RGB(0xcb, 0xbf, 0xac));
    TextOutW(dc, S(114), S(74), L"Starting the server...", 22);
    int l = S(30), r = rc.right - S(30), t = rc.bottom - S(38), b = t + S(3);
    fill(dc, l, t, r, b, RGB(0x36, 0x30, 0x28));                                      /* track */
    int seg = (r - l) / 3, pos = (splash_tick * S(5)) % (r - l + seg) - seg;
    int bl = l + pos < l ? l : l + pos, br = l + pos + seg > r ? r : l + pos + seg;
    if (br > bl)
        fill(dc, bl, t, br, b, RGB(0xe8, 0xbf, 0x6a));                                /* brass */
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
    int w = S(440), h = S(170);
    splash = CreateWindowExW(WS_EX_APPWINDOW, wc.lpszClassName, TITLE, WS_POPUP,
                             (GetSystemMetrics(SM_CXSCREEN) - w) / 2, (GetSystemMetrics(SM_CYSCREEN) - h) / 2,
                             w, h, NULL, NULL, inst, NULL);
    if (!splash)
        return;                                /* no display to show it on: carry on without */
    font_title = CreateFontW(-S(21), 0, 0, 0, FW_SEMIBOLD, 0, 0, 0, DEFAULT_CHARSET, OUT_DEFAULT_PRECIS,
                             CLIP_DEFAULT_PRECIS, CLEARTYPE_QUALITY, DEFAULT_PITCH, L"Segoe UI");
    font_sub = CreateFontW(-S(15), 0, 0, 0, FW_NORMAL, 0, 0, 0, DEFAULT_CHARSET, OUT_DEFAULT_PRECIS,
                           CLIP_DEFAULT_PRECIS, CLEARTYPE_QUALITY, DEFAULT_PITCH, L"Segoe UI");
    splash_icon = (HICON)LoadImageW(inst, MAKEINTRESOURCEW(1), IMAGE_ICON, S(64), S(64), LR_DEFAULTCOLOR);
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

/* The window: Edge as an app (no tabs, no address bar) on its own profile, with sync,
 * telemetry and SmartScreen off so it only ever talks to 127.0.0.1. Else the default browser. */
static void open_window(void)
{
    wchar_t edge[MAX_PATH], local[MAX_PATH], args[2048];
    DWORD n = GetEnvironmentVariableW(L"LOCALAPPDATA", local, MAX_PATH);
    if (n > 0 && n < MAX_PATH && find_edge(edge, MAX_PATH)
        && _snwprintf(args, 2048,
               L"--app=%ls --user-data-dir=\"%ls\\LimbusPrivateServer\\window\" --window-size=1120,760 "
               L"--no-first-run --no-default-browser-check --disable-sync --disable-background-networking "
               L"--disable-breakpad --disable-component-update --disable-domain-reliability "
               L"--disable-client-side-phishing-detection "
               L"--disable-features=msEdgeEnableSmartScreen,EdgeCollections,EdgeDiscoverFeature",
               URL, local) > 0
        && (INT_PTR)ShellExecuteW(NULL, L"open", edge, args, NULL, SW_SHOWNORMAL) > 32)
        return;
    ShellExecuteW(NULL, L"open", URL, NULL, NULL, SW_SHOWNORMAL);
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
            say(L"The server didn't start.\n\nAnother program may be using port 3000, or the folder "
                L"was only partly unzipped. The server's log is in server\\logs.");
            return 1;
        }
    }
    if (!restart && !headless) {
        open_window();
        if (splash)
            pump(1500, 0);                     /* until the window has had time to appear */
        splash_close();
    }
    WSACleanup();
    return 0;
}
