/* Limbus Private Server - the Windows installer (LimbusPrivateServer-Setup.exe).
 *
 * One window: where to install (your own user folder, no administrator), whether to turn every
 * skin on and to put a shortcut on the desktop, Install. The package - the server, the client
 * patch, the app and the skins (limbus-skins on its own bundled Python) - rides at the end of this
 * very file as a zip, with its SHA-256: it is checked before anything is written. A folder that
 * already holds the app is updated in place; the player's save, keys and friend settings stay.
 * Then a Start-menu entry, a desktop shortcut and an entry in Windows' "Installed apps", whose
 * Uninstall runs Uninstall.exe (this program without the package) from the install folder.
 *
 * The end of this file:  [stub][zip][u64 zip offset][u64 zip size][32-byte SHA-256 of the zip]["LPSSETU1"]
 *
 *   --uninstall          (Uninstall.exe) ask, then remove the app; the save stays unless you say
 *   --silent <folder>    install there without a window, every skin on (the package tests)
 *   --update <folder> [--parent <pid>]   the app's own updater: once that process has exited,
 *                        update the install there without asking (shortcuts and skin choices
 *                        stay as they are), then start the app again
 * Run by hand over an earlier install, the window offers Update instead of Install.
 *
 * Build (any OS):  zig build-lib unzip.zig -target x86_64-windows-gnu -O ReleaseSafe -fcompiler-rt
 *                  zig rc /fo setup.res setup.rc
 *                  zig cc -target x86_64-windows-gnu -O2 -municode -Wl,--subsystem,windows setup.c
 *                      setup.res unzip.lib -lshell32 -ladvapi32 -luser32 -lgdi32 -lcomctl32 -lole32
 *                      -luuid -lbcrypt -o LimbusPrivateServer-Setup.exe
 */
#define WIN32_LEAN_AND_MEAN
#ifndef UNICODE
#define UNICODE
#endif
#ifndef _UNICODE
#define _UNICODE
#endif
#define COBJMACROS
#include <windows.h>
#include <shellapi.h>
#include <shlobj.h>
#include <shobjidl.h>
#include <objbase.h>
#include <commctrl.h>
#include <bcrypt.h>
#include <stdint.h>
#include <stdio.h>
#include <stdlib.h>
#include <string.h>
#include <wchar.h>
#include "version.h"

int lps_unzip(const char *zip_path, const char *dest_path, void (*progress)(uint64_t, uint64_t));
const char *lps_unzip_error(void);

#define WIDEN2(s) L##s
#define WIDEN(s) WIDEN2(s)
static const wchar_t *TITLE = L"Limbus Private Server Setup";
static const wchar_t *APP = L"Limbus Private Server";
static const wchar_t *APP_EXE = L"Limbus Private Server.exe";
static const wchar_t *RELEASE = WIDEN(RELEASE_STR);
static const wchar_t *UNINSTALL_KEY = L"Software\\Microsoft\\Windows\\CurrentVersion\\Uninstall\\LimbusPrivateServer";
static const char MAGIC[8] = {'L', 'P', 'S', 'S', 'E', 'T', 'U', '1'};
#define TRAILER (8 + 8 + 32 + 8)
#define PORT 3000

enum { ID_DIR = 100, ID_BROWSE, ID_SKINS, ID_DESKTOP, ID_INSTALL, ID_CLOSE, ID_BAR, ID_STATUS };
enum { WM_PROGRESS = WM_APP + 1, WM_DONE };

static HWND win, bar, status_label, dir_edit, install_btn, close_btn, browse_btn, skins_box, desktop_box;
static HFONT ui_font, title_font;
static wchar_t self_path[MAX_PATH];
static uint64_t zip_offset, zip_size;
static unsigned char zip_sha[32];
static int silent, finished, updating;
static wchar_t target[MAX_PATH];        /* the install folder, no trailing backslash */
static int want_skins, want_desktop;
static wchar_t done_text[1024];

/* ---------------------------------------------------------------- small helpers */

static int file_exists(const wchar_t *p)
{
    DWORD a = GetFileAttributesW(p);
    return a != INVALID_FILE_ATTRIBUTES && !(a & FILE_ATTRIBUTE_DIRECTORY);
}

static int dir_exists(const wchar_t *p)
{
    DWORD a = GetFileAttributesW(p);
    return a != INVALID_FILE_ATTRIBUTES && (a & FILE_ATTRIBUTE_DIRECTORY);
}

static void message(const wchar_t *text, UINT icon)
{
    if (!silent)
        MessageBoxW(win, text, TITLE, MB_OK | icon | MB_SETFOREGROUND);
}

/* UTF-8 of a wide string, in a static buffer (one at a time). */
static const char *utf8(const wchar_t *w)
{
    static char buf[MAX_PATH * 4];
    if (WideCharToMultiByte(CP_UTF8, 0, w, -1, buf, sizeof buf, NULL, NULL) <= 0)
        buf[0] = 0;
    return buf;
}

/* Every folder on the way to path. */
static int make_dirs(const wchar_t *path)
{
    wchar_t p[MAX_PATH];
    if (wcslen(path) >= MAX_PATH)
        return 0;
    wcscpy(p, path);
    for (wchar_t *s = p + 3; *s; s++)
        if (*s == L'\\') {
            *s = 0;
            CreateDirectoryW(p, NULL);
            *s = L'\\';
        }
    return CreateDirectoryW(p, NULL) || GetLastError() == ERROR_ALREADY_EXISTS;
}

static void remove_tree(const wchar_t *dir)
{
    wchar_t from[MAX_PATH + 2];
    ZeroMemory(from, sizeof from);
    if (wcslen(dir) >= MAX_PATH)
        return;
    wcscpy(from, dir);                                     /* double-NUL ended */
    SHFILEOPSTRUCTW op;
    ZeroMemory(&op, sizeof op);
    op.wFunc = FO_DELETE;
    op.pFrom = from;
    op.fFlags = FOF_NO_UI;
    SHFileOperationW(&op);
}

/* Something listens on 127.0.0.1:3000 - the app's server, which holds its files open. */
static int server_running(void)
{
    if (FindWindowW(L"LimbusPrivateServerWindow", NULL))
        return 1;
    wchar_t exe[MAX_PATH];
    if (_snwprintf(exe, MAX_PATH, L"%ls\\server\\Server.exe", target) < 0)
        return 0;
    if (!file_exists(exe))
        return 0;
    /* the server's own executable can't be opened for writing while it runs */
    HANDLE h = CreateFileW(exe, GENERIC_WRITE, 0, NULL, OPEN_EXISTING, 0, NULL);
    if (h == INVALID_HANDLE_VALUE)
        return GetLastError() == ERROR_SHARING_VIOLATION;
    CloseHandle(h);
    return 0;
}

/* Run a program out of sight and wait (ms) for it. Its exit code, or -1. */
static int run_hidden(const wchar_t *exe, const wchar_t *args, const wchar_t *cwd, DWORD ms)
{
    wchar_t cmd[4096];
    if (_snwprintf(cmd, 4096, L"\"%ls\" %ls", exe, args) < 0)
        return -1;
    STARTUPINFOW si;
    PROCESS_INFORMATION pi;
    ZeroMemory(&si, sizeof si);
    si.cb = sizeof si;
    if (!CreateProcessW(exe, cmd, NULL, NULL, FALSE, CREATE_NO_WINDOW, NULL, cwd, &si, &pi))
        return -1;
    CloseHandle(pi.hThread);
    DWORD code = (DWORD)-1;
    if (WaitForSingleObject(pi.hProcess, ms) == WAIT_OBJECT_0)
        GetExitCodeProcess(pi.hProcess, &code);
    else
        TerminateProcess(pi.hProcess, 1);
    CloseHandle(pi.hProcess);
    return (int)code;
}

/* limbus-skins, on the package's own Python: python\python.exe -m limbus_skins <args>. */
static int skins(const wchar_t *dir, const wchar_t *args, DWORD ms)
{
    wchar_t py[MAX_PATH], full[1024];
    if (_snwprintf(py, MAX_PATH, L"%ls\\python\\python.exe", dir) < 0 || !file_exists(py)
        || _snwprintf(full, 1024, L"-I -X utf8 -m limbus_skins %ls", args) < 0)
        return -1;
    return run_hidden(py, full, dir, ms);
}

/* ---------------------------------------------------------------- the package at the end of this file */

static int read_trailer(void)
{
    HANDLE h = CreateFileW(self_path, GENERIC_READ, FILE_SHARE_READ, NULL, OPEN_EXISTING, 0, NULL);
    if (h == INVALID_HANDLE_VALUE)
        return 0;
    LARGE_INTEGER size, at;
    unsigned char t[TRAILER];
    DWORD got = 0;
    int ok = GetFileSizeEx(h, &size) && size.QuadPart > TRAILER;
    if (ok) {
        at.QuadPart = size.QuadPart - TRAILER;
        ok = SetFilePointerEx(h, at, NULL, FILE_BEGIN) && ReadFile(h, t, TRAILER, &got, NULL) && got == TRAILER
             && !memcmp(t + TRAILER - 8, MAGIC, 8);
    }
    CloseHandle(h);
    if (!ok)
        return 0;
    memcpy(&zip_offset, t, 8);
    memcpy(&zip_size, t + 8, 8);
    memcpy(zip_sha, t + 16, 32);
    return zip_offset > 0 && zip_size > 0 && zip_offset + zip_size + TRAILER == (uint64_t)size.QuadPart;
}

static void progress(int pct, const wchar_t *what)
{
    if (!silent)
        PostMessageW(win, WM_PROGRESS, (WPARAM)pct, (LPARAM)_wcsdup(what));
}

/* Bytes [from, from+n) of this file into out, through SHA-256; 1 when the hash is want. */
static int copy_checked(uint64_t from, uint64_t n, const wchar_t *out, const unsigned char *want, uint64_t stop_at)
{
    HANDLE in = CreateFileW(self_path, GENERIC_READ, FILE_SHARE_READ, NULL, OPEN_EXISTING, FILE_FLAG_SEQUENTIAL_SCAN, NULL);
    if (in == INVALID_HANDLE_VALUE)
        return 0;
    HANDLE o = CreateFileW(out, GENERIC_WRITE, 0, NULL, CREATE_ALWAYS, FILE_ATTRIBUTE_TEMPORARY, NULL);
    if (o == INVALID_HANDLE_VALUE) {
        CloseHandle(in);
        return 0;
    }
    BCRYPT_ALG_HANDLE alg = NULL;
    BCRYPT_HASH_HANDLE hash = NULL;
    int ok = want == NULL
             || (BCryptOpenAlgorithmProvider(&alg, BCRYPT_SHA256_ALGORITHM, NULL, 0) == 0
                 && BCryptCreateHash(alg, &hash, NULL, 0, NULL, 0, 0) == 0);
    LARGE_INTEGER at;
    at.QuadPart = (LONGLONG)from;
    ok = ok && SetFilePointerEx(in, at, NULL, FILE_BEGIN);
    static unsigned char buf[1 << 20];
    uint64_t left = n;
    while (ok && left) {
        DWORD want_n = left > sizeof buf ? sizeof buf : (DWORD)left, got = 0, put = 0;
        ok = ReadFile(in, buf, want_n, &got, NULL) && got == want_n && WriteFile(o, buf, got, &put, NULL) && put == got
             && (!hash || BCryptHashData(hash, buf, got, 0) == 0);
        left -= got;
        if (stop_at)
            progress((int)(30 * (n - left) / n), L"Checking the download...");
    }
    unsigned char sum[32];
    if (ok && hash)
        ok = BCryptFinishHash(hash, sum, 32, 0) == 0 && !memcmp(sum, want, 32);
    if (hash) BCryptDestroyHash(hash);
    if (alg) BCryptCloseAlgorithmProvider(alg, 0);
    CloseHandle(in);
    CloseHandle(o);
    if (!ok)
        DeleteFileW(out);
    return ok;
}

static void unzip_progress(uint64_t done, uint64_t total)
{
    static int last = -1;
    int pct = 30 + (int)(50 * done / (total ? total : 1));
    if (pct != last) {
        last = pct;
        progress(pct, L"Unpacking...");
    }
}

/* The player's own files stay: their save, keys and client settings (as the app's updates do). */
static int keep_theirs(const wchar_t *rel)
{
    size_t n = wcslen(rel);
    return !_wcsicmp(rel, L"client\\myosotis.ini") || !_wcsicmp(rel, L"join\\myosotis.ini")
           || (n > 4 && !_wcsicmp(rel + n - 4, L".key")) || !_wcsnicmp(rel, L"server\\myosotis.db", 18);
}

/* Move src\* over dst\*, keeping the player's own files. 0 when something couldn't be written. */
static int move_tree(const wchar_t *src, const wchar_t *dst, const wchar_t *rel, wchar_t *failed)
{
    wchar_t pat[MAX_PATH];
    if (_snwprintf(pat, MAX_PATH, L"%ls\\*", src) < 0)
        return 0;
    WIN32_FIND_DATAW fd;
    HANDLE h = FindFirstFileW(pat, &fd);
    if (h == INVALID_HANDLE_VALUE)
        return 0;
    if (!make_dirs(dst)) {
        FindClose(h);
        wcsncpy(failed, dst, MAX_PATH - 1);
        return 0;
    }
    int ok = 1;
    do {
        if (!wcscmp(fd.cFileName, L".") || !wcscmp(fd.cFileName, L".."))
            continue;
        wchar_t s[MAX_PATH], d[MAX_PATH], r[MAX_PATH], old[MAX_PATH];
        if (_snwprintf(s, MAX_PATH, L"%ls\\%ls", src, fd.cFileName) < 0
            || _snwprintf(d, MAX_PATH, L"%ls\\%ls", dst, fd.cFileName) < 0
            || _snwprintf(r, MAX_PATH, rel[0] ? L"%ls\\%ls" : L"%ls%ls", rel, fd.cFileName) < 0) {
            ok = 0;
            break;
        }
        if (fd.dwFileAttributes & FILE_ATTRIBUTE_DIRECTORY) {
            if (!move_tree(s, d, r, failed)) { ok = 0; break; }
            continue;
        }
        if (keep_theirs(r) && file_exists(d))
            continue;
        if (!MoveFileExW(s, d, MOVEFILE_REPLACE_EXISTING | MOVEFILE_COPY_ALLOWED)) {
            /* in use (a running uninstaller, a stray python): aside to .old, which Windows allows */
            if (_snwprintf(old, MAX_PATH, L"%ls.old", d) < 0) { ok = 0; break; }
            DeleteFileW(old);
            if (!MoveFileExW(d, old, MOVEFILE_REPLACE_EXISTING)
                || !MoveFileExW(s, d, MOVEFILE_REPLACE_EXISTING | MOVEFILE_COPY_ALLOWED)) {
                wcsncpy(failed, r, MAX_PATH - 1);
                ok = 0;
                break;
            }
        }
    } while (FindNextFileW(h, &fd));
    FindClose(h);
    return ok;
}

/* A .lnk at link pointing at target (in dir). */
static int shortcut(const wchar_t *link, const wchar_t *exe, const wchar_t *args, const wchar_t *dir, const wchar_t *about)
{
    IShellLinkW *sl = NULL;
    IPersistFile *pf = NULL;
    int ok = 0;
    if (SUCCEEDED(CoCreateInstance(&CLSID_ShellLink, NULL, CLSCTX_INPROC_SERVER, &IID_IShellLinkW, (void **)&sl))) {
        IShellLinkW_SetPath(sl, exe);
        if (args) IShellLinkW_SetArguments(sl, args);
        IShellLinkW_SetWorkingDirectory(sl, dir);
        IShellLinkW_SetDescription(sl, about);
        IShellLinkW_SetIconLocation(sl, exe, 0);
        if (SUCCEEDED(IShellLinkW_QueryInterface(sl, &IID_IPersistFile, (void **)&pf))) {
            ok = SUCCEEDED(IPersistFile_Save(pf, link, TRUE));
            IPersistFile_Release(pf);
        }
        IShellLinkW_Release(sl);
    }
    return ok;
}

static int special(int csidl, wchar_t *out)
{
    return SUCCEEDED(SHGetFolderPathW(NULL, csidl, NULL, SHGFP_TYPE_CURRENT, out));
}

static void link_paths(wchar_t *start, wchar_t *desk)
{
    wchar_t p[MAX_PATH];
    start[0] = desk[0] = 0;
    if (special(CSIDL_PROGRAMS, p))
        _snwprintf(start, MAX_PATH, L"%ls\\%ls.lnk", p, APP);
    if (special(CSIDL_DESKTOPDIRECTORY, p))
        _snwprintf(desk, MAX_PATH, L"%ls\\%ls.lnk", p, APP);
}

static void reg_str(HKEY k, const wchar_t *name, const wchar_t *v)
{
    RegSetValueExW(k, name, 0, REG_SZ, (const BYTE *)v, (DWORD)((wcslen(v) + 1) * sizeof(wchar_t)));
}

static void reg_dword(HKEY k, const wchar_t *name, DWORD v)
{
    RegSetValueExW(k, name, 0, REG_DWORD, (const BYTE *)&v, sizeof v);
}

/* The install's size in KB, for "Installed apps". */
static uint64_t tree_size(const wchar_t *dir)
{
    wchar_t pat[MAX_PATH];
    uint64_t total = 0;
    if (_snwprintf(pat, MAX_PATH, L"%ls\\*", dir) < 0)
        return 0;
    WIN32_FIND_DATAW fd;
    HANDLE h = FindFirstFileW(pat, &fd);
    if (h == INVALID_HANDLE_VALUE)
        return 0;
    do {
        if (!wcscmp(fd.cFileName, L".") || !wcscmp(fd.cFileName, L".."))
            continue;
        if (fd.dwFileAttributes & FILE_ATTRIBUTE_DIRECTORY) {
            wchar_t sub[MAX_PATH];
            if (_snwprintf(sub, MAX_PATH, L"%ls\\%ls", dir, fd.cFileName) > 0)
                total += tree_size(sub);
        } else {
            total += ((uint64_t)fd.nFileSizeHigh << 32) | fd.nFileSizeLow;
        }
    } while (FindNextFileW(h, &fd));
    FindClose(h);
    return total;
}

/* ---------------------------------------------------------------- installing */

static int fail(const wchar_t *fmt, const wchar_t *arg)
{
    _snwprintf(done_text, 1024, fmt, arg);
    return 0;
}

static int install(void)
{
    wchar_t tmp[MAX_PATH], zip[MAX_PATH], stage[MAX_PATH], src[MAX_PATH], failed[MAX_PATH] = L"";
    wchar_t exe[MAX_PATH], unin[MAX_PATH], start[MAX_PATH], desk[MAX_PATH], cmd[MAX_PATH + 32];
    DWORD pid = GetCurrentProcessId();

    if (server_running())
        return fail(L"Limbus Private Server is running. Press \"Stop server\" in its window (and close Limbus), "
                    L"then Install again.%ls", L"");
    if (!make_dirs(target))
        return fail(L"Can't create the folder\n%ls\n\nPick another one.", target);

    /* 1. the package out of this file, checked against its SHA-256 */
    progress(0, L"Checking the download...");
    if (!GetTempPathW(MAX_PATH, tmp) || _snwprintf(zip, MAX_PATH, L"%lslps-setup-%lu.zip", tmp, (unsigned long)pid) < 0)
        return fail(L"No temporary folder.%ls", L"");
    if (!copy_checked(zip_offset, zip_size, zip, zip_sha, 1))
        return fail(L"The download is damaged (its checksum doesn't match) or the disk is full.\n\n"
                    L"Download LimbusPrivateServer-Setup.exe again.%ls", L"");

    /* 2. unpacked beside the install folder (the same drive: the move is a rename) */
    if (_snwprintf(stage, MAX_PATH, L"%ls.setup-%lu", target, (unsigned long)pid) < 0) {
        DeleteFileW(zip);
        return fail(L"The install folder's name is too long.%ls", L"");
    }
    remove_tree(stage);
    CreateDirectoryW(stage, NULL);
    progress(30, L"Unpacking...");
    char zip8[MAX_PATH * 4];
    strcpy(zip8, utf8(zip));
    int bad = lps_unzip(zip8, utf8(stage), unzip_progress);
    DeleteFileW(zip);
    if (bad) {
        wchar_t why[200];
        MultiByteToWideChar(CP_UTF8, 0, lps_unzip_error(), -1, why, 200);
        remove_tree(stage);
        return fail(L"Couldn't unpack the package (%ls). Is the disk full?", why);
    }

    /* 3. into place: an earlier install is updated, its save and settings kept */
    progress(82, L"Copying into place...");
    if (_snwprintf(src, MAX_PATH, L"%ls\\LimbusPrivateServer", stage) < 0 || !move_tree(src, target, L"", failed)) {
        remove_tree(stage);
        return fail(L"Couldn't write %ls into the install folder. Close Limbus Private Server and anything "
                    L"using that folder, then Install again.", failed[0] ? failed : L"a file");
    }
    remove_tree(stage);

    /* 4. the uninstaller: this program without the package */
    progress(88, L"Shortcuts...");
    if (_snwprintf(exe, MAX_PATH, L"%ls\\%ls", target, APP_EXE) < 0
        || _snwprintf(unin, MAX_PATH, L"%ls\\Uninstall.exe", target) < 0
        || !copy_checked(0, zip_offset, unin, NULL, 0))
        return fail(L"Couldn't write Uninstall.exe into the install folder.%ls", L"");

    /* 5. shortcuts and the "Installed apps" entry */
    link_paths(start, desk);
    if (start[0])
        shortcut(start, exe, NULL, target, L"Play Limbus Company on your own private server");
    if (desk[0] && !updating) {
        if (want_desktop)
            shortcut(desk, exe, NULL, target, L"Play Limbus Company on your own private server");
        else
            DeleteFileW(desk);
    }
    HKEY k;
    if (RegCreateKeyExW(HKEY_CURRENT_USER, UNINSTALL_KEY, 0, NULL, 0, KEY_WRITE, NULL, &k, NULL) == ERROR_SUCCESS) {
        _snwprintf(cmd, MAX_PATH + 32, L"\"%ls\" --uninstall", unin);
        reg_str(k, L"DisplayName", APP);
        reg_str(k, L"DisplayVersion", RELEASE);
        reg_str(k, L"DisplayIcon", exe);
        reg_str(k, L"InstallLocation", target);
        reg_str(k, L"UninstallString", cmd);
        wcscat(cmd, L" --quiet");
        reg_str(k, L"QuietUninstallString", cmd);
        reg_dword(k, L"NoModify", 1);
        reg_dword(k, L"NoRepair", 1);
        reg_dword(k, L"EstimatedSize", (DWORD)(tree_size(target) / 1024));
        RegCloseKey(k);
    }

    /* 6. the skins: every one on, when asked (they're built on first Play) */
    wchar_t note[300] = L"";
    if (want_skins && !updating) {
        progress(92, L"Turning the skins on...");
        int rc = skins(target, L"on all", 120000);
        if (rc != 0)
            _snwprintf(note, 300, L"\n\nThe skins couldn't be turned on (code %d) - open the app's Skins window to pick them.", rc);
    }
    progress(100, L"Done.");
    if (updating) {
        _snwprintf(done_text, 1024, L"Updated to %ls in\n%ls\n\nYour save, keys and friend settings stayed.", RELEASE, target);
        return 1;
    }
    _snwprintf(done_text, 1024,
               L"Installed in\n%ls\n\nOpen Limbus Private Server from the Start menu%ls and press Play. The first Play "
               L"with skins builds them - that takes a few minutes, once.%ls",
               target, want_desktop ? L" or your desktop" : L"", note);
    return 1;
}

static DWORD WINAPI install_thread(LPVOID unused)
{
    (void)unused;
    CoInitializeEx(NULL, COINIT_APARTMENTTHREADED);
    int ok = install();
    CoUninitialize();
    if (!silent)
        PostMessageW(win, WM_DONE, ok, 0);
    return ok ? 0 : 1;
}

/* ---------------------------------------------------------------- uninstalling */

/* Delete dir's files but the player's save and keys (server\\myosotis.db*, *.key), then the
 * folders left empty. 1 when only those stayed. */
static int saved_file(const wchar_t *rel)
{
    size_t n = wcslen(rel);
    return (n > 4 && !_wcsicmp(rel + n - 4, L".key")) || !_wcsnicmp(rel, L"server\\myosotis.db", 18);
}

static int clear_tree(const wchar_t *dir, const wchar_t *rel)
{
    wchar_t pat[MAX_PATH];
    if (_snwprintf(pat, MAX_PATH, L"%ls\\*", dir) < 0)
        return 0;
    WIN32_FIND_DATAW fd;
    HANDLE h = FindFirstFileW(pat, &fd);
    if (h == INVALID_HANDLE_VALUE)
        return 1;
    int clean = 1;
    do {
        if (!wcscmp(fd.cFileName, L".") || !wcscmp(fd.cFileName, L".."))
            continue;
        wchar_t p[MAX_PATH], r[MAX_PATH];
        if (_snwprintf(p, MAX_PATH, L"%ls\\%ls", dir, fd.cFileName) < 0
            || _snwprintf(r, MAX_PATH, rel[0] ? L"%ls\\%ls" : L"%ls%ls", rel, fd.cFileName) < 0) {
            clean = 0;
            continue;
        }
        if (fd.dwFileAttributes & FILE_ATTRIBUTE_DIRECTORY) {
            if (clear_tree(p, r))
                RemoveDirectoryW(p);
            else
                clean = 0;
        } else if (saved_file(r)) {
            clean = 0;
        } else {
            SetFileAttributesW(p, FILE_ATTRIBUTE_NORMAL);
            if (!DeleteFileW(p))
                clean = 0;
        }
    } while (FindNextFileW(h, &fd));
    FindClose(h);
    return clean;
}

static int uninstall(const wchar_t *dir, int quiet)
{
    wchar_t p[MAX_PATH], start[MAX_PATH], desk[MAX_PATH], local[MAX_PATH];
    wcsncpy(target, dir, MAX_PATH - 1);
    if (server_running()) {
        if (!quiet)
            MessageBoxW(NULL, L"Limbus Private Server is running. Press \"Stop server\" in its window (and close "
                        L"Limbus), then uninstall again.", TITLE, MB_OK | MB_ICONWARNING);
        return 1;
    }
    int keep = 1;
    if (!quiet) {
        if (MessageBoxW(NULL, L"Remove Limbus Private Server?", TITLE, MB_YESNO | MB_ICONQUESTION) != IDYES)
            return 1;
        keep = MessageBoxW(NULL, L"Keep your save, your keys and your skin choices, for a later install?\n\n"
                           L"Yes: keep them.   No: delete everything.", TITLE, MB_YESNO | MB_ICONQUESTION) == IDYES;
    }
    /* the game's own files as they ship (limbus-skins checks them), before its Python goes */
    skins(dir, L"restore", 120000);

    link_paths(start, desk);
    if (start[0]) DeleteFileW(start);
    if (desk[0]) DeleteFileW(desk);
    RegDeleteKeyW(HKEY_CURRENT_USER, UNINSTALL_KEY);
    if (GetEnvironmentVariableW(L"LOCALAPPDATA", local, MAX_PATH)
        && _snwprintf(p, MAX_PATH, L"%ls\\LimbusPrivateServer", local) > 0)
        remove_tree(p);                                  /* the mod menu's window profile */

    if (keep) {
        clear_tree(dir, L"");                             /* everything but the save and keys */
    } else {
        remove_tree(dir);
        if (GetEnvironmentVariableW(L"APPDATA", local, MAX_PATH) && _snwprintf(p, MAX_PATH, L"%ls\\limbus-skins", local) > 0)
            remove_tree(p);
        if (GetEnvironmentVariableW(L"LOCALAPPDATA", local, MAX_PATH) && _snwprintf(p, MAX_PATH, L"%ls\\limbus-skins", local) > 0)
            remove_tree(p);
    }
    if (!quiet)
        MessageBoxW(NULL, keep ? L"Limbus Private Server is removed. Your save stayed in its folder; installing "
                                 L"there again picks it up."
                               : L"Limbus Private Server is removed.", TITLE, MB_OK | MB_ICONINFORMATION);
    return 0;
}

/* Uninstall.exe lives in the folder it removes: it runs again from a copy in %TEMP%. */
static int uninstall_from_copy(int quiet)
{
    wchar_t dir[MAX_PATH], tmp[MAX_PATH], copy[MAX_PATH], cmd[MAX_PATH * 3];
    wcsncpy(dir, self_path, MAX_PATH - 1);
    dir[MAX_PATH - 1] = 0;
    wchar_t *slash = wcsrchr(dir, L'\\');
    if (!slash)
        return 1;
    *slash = 0;
    if (!GetTempPathW(MAX_PATH, tmp)
        || _snwprintf(copy, MAX_PATH, L"%lslps-uninstall-%lu.exe", tmp, (unsigned long)GetCurrentProcessId()) < 0
        || !CopyFileW(self_path, copy, FALSE)
        || _snwprintf(cmd, MAX_PATH * 3, L"\"%ls\" --parent %lu --uninstall-from \"%ls\"%ls", copy,
                      (unsigned long)GetCurrentProcessId(), dir, quiet ? L" --quiet" : L"") < 0)
        return uninstall(dir, quiet);
    STARTUPINFOW si;
    PROCESS_INFORMATION pi;
    ZeroMemory(&si, sizeof si);
    si.cb = sizeof si;
    if (!CreateProcessW(copy, cmd, NULL, NULL, FALSE, 0, NULL, tmp, &si, &pi))
        return uninstall(dir, quiet);
    CloseHandle(pi.hThread);
    CloseHandle(pi.hProcess);
    return 0;
}

/* ---------------------------------------------------------------- the window */

static HWND control(const wchar_t *cls, const wchar_t *text, DWORD style, int x, int y, int w, int h, int id)
{
    HWND c = CreateWindowExW(wcscmp(cls, L"EDIT") == 0 ? WS_EX_CLIENTEDGE : 0, cls, text,
                             WS_CHILD | WS_VISIBLE | style, x, y, w, h, win, (HMENU)(INT_PTR)id, NULL, NULL);
    SendMessageW(c, WM_SETFONT, (WPARAM)ui_font, TRUE);
    return c;
}

static void default_dir(wchar_t *out)
{
    wchar_t home[MAX_PATH], p[MAX_PATH];
    DWORD n = MAX_PATH;
    HKEY k;
    out[0] = 0;
    /* an earlier install: update it where it is */
    if (RegOpenKeyExW(HKEY_CURRENT_USER, UNINSTALL_KEY, 0, KEY_READ, &k) == ERROR_SUCCESS) {
        DWORD type, size = sizeof p;
        if (RegQueryValueExW(k, L"InstallLocation", NULL, &type, (BYTE *)p, &size) == ERROR_SUCCESS && type == REG_SZ
            && dir_exists(p))
            wcsncpy(out, p, MAX_PATH - 1);
        RegCloseKey(k);
        if (out[0]) {
            wchar_t exe[MAX_PATH];
            _snwprintf(exe, MAX_PATH, L"%ls\\%ls", out, APP_EXE);
            updating = file_exists(exe);
            return;
        }
    }
    if (!GetEnvironmentVariableW(L"USERPROFILE", home, n))
        wcscpy(home, L"C:\\");
    _snwprintf(out, MAX_PATH, L"%ls\\Games\\LimbusPrivateServer", home);
}

static void browse(void)
{
    BROWSEINFOW bi;
    ZeroMemory(&bi, sizeof bi);
    bi.hwndOwner = win;
    bi.lpszTitle = L"Install Limbus Private Server in:";
    bi.ulFlags = BIF_RETURNONLYFSDIRS | BIF_NEWDIALOGSTYLE;
    PIDLIST_ABSOLUTE id = SHBrowseForFolderW(&bi);
    if (!id)
        return;
    wchar_t p[MAX_PATH], full[MAX_PATH];
    if (SHGetPathFromIDListW(id, p)) {
        size_t n = wcslen(p);
        if (n && p[n - 1] == L'\\')
            p[n - 1] = 0;
        /* a folder of its own inside the one picked, unless it is one already */
        wchar_t probe[MAX_PATH];
        _snwprintf(probe, MAX_PATH, L"%ls\\%ls", p, APP_EXE);
        if (file_exists(probe) || _snwprintf(full, MAX_PATH, L"%ls\\LimbusPrivateServer", p) < 0)
            wcscpy(full, p);
        SetWindowTextW(dir_edit, full);
    }
    CoTaskMemFree(id);
}

static void set_busy(int busy)
{
    EnableWindow(install_btn, !busy);
    EnableWindow(browse_btn, !busy);
    EnableWindow(dir_edit, !busy);
    EnableWindow(skins_box, !busy);
    EnableWindow(desktop_box, !busy);
    EnableWindow(close_btn, !busy);
}

/* What was typed: a full path, no trailing backslash; 0 if it isn't one. */
static int read_target(void)
{
    wchar_t typed[MAX_PATH];
    GetWindowTextW(dir_edit, typed, MAX_PATH);
    wchar_t *s = typed;
    while (*s == L' ' || *s == L'"') s++;
    size_t n = wcslen(s);
    while (n && (s[n - 1] == L' ' || s[n - 1] == L'"' || s[n - 1] == L'\\')) s[--n] = 0;
    if (n < 3 || !GetFullPathNameW(s, MAX_PATH, target, NULL))
        return 0;
    if (wcslen(target) > 150)                               /* room for the package's deepest file */
        return 0;
    return target[1] == L':' && target[2] == L'\\';
}

static LRESULT CALLBACK window_proc(HWND w, UINT msg, WPARAM wp, LPARAM lp)
{
    switch (msg) {
    case WM_COMMAND:
        if (HIWORD(wp) != BN_CLICKED)
            break;
        switch (LOWORD(wp)) {
        case ID_BROWSE:
            browse();
            return 0;
        case ID_INSTALL:
            if (finished) {
                wchar_t exe[MAX_PATH];
                _snwprintf(exe, MAX_PATH, L"%ls\\%ls", target, APP_EXE);
                ShellExecuteW(NULL, L"open", exe, NULL, target, SW_SHOWNORMAL);
                DestroyWindow(w);
                return 0;
            }
            if (!read_target()) {
                message(L"Pick a folder on a drive (like C:\\Users\\you\\Games\\LimbusPrivateServer), "
                        L"with a path shorter than 150 characters.", MB_ICONWARNING);
                return 0;
            }
            want_skins = IsDlgButtonChecked(w, ID_SKINS) == BST_CHECKED;
            want_desktop = IsDlgButtonChecked(w, ID_DESKTOP) == BST_CHECKED;
            set_busy(1);
            HANDLE t = CreateThread(NULL, 0, install_thread, NULL, 0, NULL);
            if (t)
                CloseHandle(t);
            else
                set_busy(0);
            return 0;
        case ID_CLOSE:
            DestroyWindow(w);
            return 0;
        }
        break;
    case WM_PROGRESS: {
        SendMessageW(bar, PBM_SETPOS, wp, 0);
        wchar_t *what = (wchar_t *)lp;
        if (what) {
            SetWindowTextW(status_label, what);
            free(what);
        }
        return 0;
    }
    case WM_DONE:
        set_busy(0);
        if (wp) {
            finished = 1;
            SetWindowTextW(install_btn, L"&Open the app");
            SetWindowTextW(close_btn, L"&Close");
            EnableWindow(browse_btn, FALSE);
            EnableWindow(dir_edit, FALSE);
            EnableWindow(skins_box, FALSE);
            EnableWindow(desktop_box, FALSE);
            SetWindowTextW(status_label, updating ? L"Updated." : L"Installed.");
            MessageBoxW(w, done_text, TITLE, MB_OK | MB_ICONINFORMATION);
        } else {
            SendMessageW(bar, PBM_SETPOS, 0, 0);
            SetWindowTextW(status_label, L"Not installed - see the message.");
            MessageBoxW(w, done_text, TITLE, MB_OK | MB_ICONWARNING);
        }
        return 0;
    case WM_CTLCOLORSTATIC:
        SetBkMode((HDC)wp, TRANSPARENT);
        return (LRESULT)GetSysColorBrush(COLOR_BTNFACE);
    case WM_CLOSE:
        if (!IsWindowEnabled(close_btn) && !finished)
            return 0;                                       /* not halfway through an install */
        DestroyWindow(w);
        return 0;
    case WM_DESTROY:
        PostQuitMessage(0);
        return 0;
    }
    return DefWindowProcW(w, msg, wp, lp);
}

static int window(HINSTANCE inst)
{
    INITCOMMONCONTROLSEX icc = {sizeof icc, ICC_PROGRESS_CLASS | ICC_STANDARD_CLASSES};
    InitCommonControlsEx(&icc);
    NONCLIENTMETRICSW ncm;
    ncm.cbSize = sizeof ncm;
    LOGFONTW big;
    if (SystemParametersInfoW(SPI_GETNONCLIENTMETRICS, sizeof ncm, &ncm, 0)) {
        ui_font = CreateFontIndirectW(&ncm.lfMessageFont);
        big = ncm.lfMessageFont;
        big.lfHeight = big.lfHeight * 3 / 2;
        big.lfWeight = FW_BOLD;
        title_font = CreateFontIndirectW(&big);
    } else {
        ui_font = title_font = (HFONT)GetStockObject(DEFAULT_GUI_FONT);
    }
    WNDCLASSW wc;
    ZeroMemory(&wc, sizeof wc);
    wc.lpfnWndProc = window_proc;
    wc.hInstance = inst;
    wc.hIcon = LoadIconW(inst, MAKEINTRESOURCEW(1));
    wc.hCursor = LoadCursorW(NULL, IDC_ARROW);
    wc.hbrBackground = (HBRUSH)(COLOR_BTNFACE + 1);
    wc.lpszClassName = L"LimbusPrivateServerSetup";
    RegisterClassW(&wc);
    RECT r = {0, 0, 520, 300};
    DWORD style = WS_OVERLAPPED | WS_CAPTION | WS_SYSMENU | WS_MINIMIZEBOX;
    AdjustWindowRect(&r, style, FALSE);
    win = CreateWindowExW(0, wc.lpszClassName, TITLE, style, CW_USEDEFAULT, CW_USEDEFAULT,
                          r.right - r.left, r.bottom - r.top, NULL, NULL, inst, NULL);
    if (!win)
        return 1;
    wchar_t head[128], dir[MAX_PATH];
    default_dir(dir);
    _snwprintf(head, 128, updating ? L"Update Limbus Private Server to %ls" : L"Limbus Private Server %ls", RELEASE);
    HWND t = control(L"STATIC", head, SS_LEFT, 16, 12, 488, 30, 0);
    SendMessageW(t, WM_SETFONT, (WPARAM)title_font, TRUE);
    control(L"STATIC", L"Your own Limbus Company server, its client patch and the skins (limbus-skins), installed "
            L"for your Windows user - no administrator needed. Your Steam copy of Limbus is never changed.",
            SS_LEFT, 16, 46, 488, 36, 0);
    control(L"STATIC", updating ? L"Installed in (your save, keys and friend settings stay):" : L"Install to:",
            SS_LEFT, 16, 96, 488, 18, 0);
    dir_edit = control(L"EDIT", dir, WS_TABSTOP | ES_AUTOHSCROLL, 16, 116, 392, 24, ID_DIR);
    browse_btn = control(L"BUTTON", L"&Browse...", WS_TABSTOP | BS_PUSHBUTTON, 416, 115, 88, 26, ID_BROWSE);
    skins_box = control(L"BUTTON", L"Turn every &skin on (pick them later in the app's Skins window)",
                        WS_TABSTOP | BS_AUTOCHECKBOX, 16, 150, 488, 22, ID_SKINS);
    desktop_box = control(L"BUTTON", L"A shortcut on the &desktop", WS_TABSTOP | BS_AUTOCHECKBOX, 16, 174, 488, 22, ID_DESKTOP);
    CheckDlgButton(win, ID_SKINS, BST_CHECKED);
    CheckDlgButton(win, ID_DESKTOP, BST_CHECKED);
    bar = CreateWindowExW(0, PROGRESS_CLASSW, NULL, WS_CHILD | WS_VISIBLE, 16, 208, 488, 18, win, (HMENU)ID_BAR, inst, NULL);
    SendMessageW(bar, PBM_SETRANGE, 0, MAKELPARAM(0, 100));
    status_label = control(L"STATIC", L"Ready. Limbus should have been started once through Steam.", SS_LEFT, 16, 230, 488, 18, ID_STATUS);
    install_btn = control(L"BUTTON", updating ? L"&Update" : L"&Install", WS_TABSTOP | BS_DEFPUSHBUTTON, 300, 260, 100, 28, ID_INSTALL);
    if (updating) {                                       /* the choices made at install time stand */
        ShowWindow(skins_box, SW_HIDE);
        ShowWindow(desktop_box, SW_HIDE);
    }
    close_btn = control(L"BUTTON", L"Cancel", WS_TABSTOP | BS_PUSHBUTTON, 404, 260, 100, 28, ID_CLOSE);
    ShowWindow(win, SW_SHOWNORMAL);
    SetForegroundWindow(win);
    SetFocus(install_btn);
    MSG m;
    while (GetMessageW(&m, NULL, 0, 0) > 0)
        if (!IsDialogMessageW(win, &m)) {
            TranslateMessage(&m);
            DispatchMessageW(&m);
        }
    return 0;
}

/* The argument after flag in cmdline (quoted or not), into out. */
static int arg_after(const wchar_t *cmdline, const wchar_t *flag, wchar_t *out)
{
    const wchar_t *p = wcsstr(cmdline, flag);
    if (!p)
        return 0;
    p += wcslen(flag);
    while (*p == L' ') p++;
    wchar_t end = L' ';
    if (*p == L'"') { end = L'"'; p++; }
    int i = 0;
    while (*p && *p != end && i < MAX_PATH - 1) out[i++] = *p++;
    out[i] = 0;
    return i > 0;
}

int WINAPI wWinMain(HINSTANCE inst, HINSTANCE prev, PWSTR cmdline, int show)
{
    (void)prev; (void)show;
    DWORD n = GetModuleFileNameW(NULL, self_path, MAX_PATH);
    if (n == 0 || n >= MAX_PATH)
        return 1;
    wchar_t dir[MAX_PATH];
    int quiet = wcsstr(cmdline, L"--quiet") != NULL;
    if (arg_after(cmdline, L"--uninstall-from", dir)) {
        wchar_t pid[32];
        if (arg_after(cmdline, L"--parent", pid)) {         /* Uninstall.exe itself: gone once it exits */
            HANDLE parent = OpenProcess(SYNCHRONIZE, FALSE, (DWORD)wcstoul(pid, NULL, 10));
            if (parent) {
                WaitForSingleObject(parent, 15000);
                CloseHandle(parent);
            }
        }
        return uninstall(dir, quiet);
    }
    if (wcsstr(cmdline, L"--uninstall"))
        return uninstall_from_copy(quiet);
    if (!read_trailer()) {
        MessageBoxW(NULL, L"This file is incomplete - download LimbusPrivateServer-Setup.exe again.\n\n"
                    L"(Uninstall.exe removes an install: run it from \"Installed apps\".)", TITLE, MB_OK | MB_ICONERROR);
        return 1;
    }
    if (arg_after(cmdline, L"--update", dir)) {
        wchar_t pid[32], exe[MAX_PATH];
        if (arg_after(cmdline, L"--parent", pid)) {         /* the app: let it close first */
            HANDLE parent = OpenProcess(SYNCHRONIZE, FALSE, (DWORD)wcstoul(pid, NULL, 10));
            if (parent) {
                WaitForSingleObject(parent, 30000);
                CloseHandle(parent);
            }
        }
        if (!GetFullPathNameW(dir, MAX_PATH, target, NULL))
            return 1;
        silent = updating = 1;
        for (int i = 0; i < 30 && server_running(); i++)     /* the app stopped it a moment ago */
            Sleep(1000);
        int rc = install_thread(NULL);
        if (rc)
            MessageBoxW(NULL, done_text, TITLE, MB_OK | MB_ICONWARNING);
        _snwprintf(exe, MAX_PATH, L"%ls\\%ls", target, APP_EXE);
        ShellExecuteW(NULL, L"open", exe, NULL, target, SW_SHOWNORMAL);   /* the new app, or the old one if it failed */
        return rc;
    }
    if (arg_after(cmdline, L"--silent", dir)) {
        silent = 1;
        if (!GetFullPathNameW(dir, MAX_PATH, target, NULL))
            return 1;
        want_skins = 1;
        want_desktop = 0;
        int rc = install_thread(NULL);
        if (rc) {
            HANDLE e = GetStdHandle(STD_ERROR_HANDLE);
            DWORD put;
            const char *t = utf8(done_text);
            if (e && e != INVALID_HANDLE_VALUE)
                WriteFile(e, t, (DWORD)strlen(t), &put, NULL);
        }
        return rc;
    }
    CoInitializeEx(NULL, COINIT_APARTMENTTHREADED);
    int rc = window(inst);
    CoUninitialize();
    return rc;
}
