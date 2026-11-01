# Install in one-liners

Your own server on your own PC, with the client patch and (optionally) the skins: one command
to install, one to play. Made for **Limbus 1.118.0**: the server package `app-1.118.0` and
limbus-skins 1.5.0. Your Steam copy of Limbus is never modified.

**First, on any system:** install Limbus through Steam, start it once up to the title screen,
then close it.

Not tested on every system listed. If a step fails, the package's `README.txt` has the
details, and `check` (below) says what it can't find.

---

## Linux (Steam with Proton, native or Flatpak)

### 1. Download and install - one file

```sh
cd ~/Downloads && curl -fLO https://github.com/paladiga484/myosotis-server/releases/download/app-1.118.0/LimbusPrivateServer-Linux-1.118.0.run && curl -fLO https://github.com/paladiga484/myosotis-server/releases/download/app-1.118.0/LimbusPrivateServer-Linux-1.118.0.run.sha256 && sha256sum -c LimbusPrivateServer-Linux-1.118.0.run.sha256 && bash LimbusPrivateServer-Linux-1.118.0.run
```

(Or download the `.run` from the release page and double-click it - choose Run if asked. `curl` is
missing? `sudo apt install curl` on Mint, Ubuntu, Debian, Pop!_OS; `sudo dnf install curl` on
Fedora; `sudo pacman -S curl` on Arch, EndeavourOS, CachyOS, Manjaro.)

It checks itself, then asks in windows on a desktop (zenity or kdialog), else in the terminal, and
puts everything in your home folder: the server in `~/.local/share/LimbusPrivateServer`, the
command `limbus`, entries in your app menu - **Limbus Private Server**, **Limbus Manager** and
**Limbus Mod Manager** - and the skins (limbus-skins, in a Python folder of its own; if Python is
missing it offers your distribution's package). It checks for Steam, Limbus and Proton first.
After that, the download can go. The `.tar.gz` is the same thing unpacked: `./install.sh` in it.

### 2. The manager

Type `limbus` in a terminal (or open **Limbus Manager**): a numbered menu - play, skins & allies
(toggle them by number, `/name` to search), the mod menu, the server's tools, and the source on
GitHub.

### 3. Play (every time)

Open **Limbus Private Server** from your app menu, or `limbus play` (01 in the menu). It swaps the
skins in (slow only the first time), starts the server and Limbus, attaches the patch, and puts
the game's own files back when Limbus closes.

`limbus help` lists the rest: `limbus menu` (the mod menu, while it runs), `limbus check`,
`limbus plain` (no skins), `limbus skins list`, `limbus uninstall` (keeps your save unless you
say otherwise).

Without installing, everything also runs from the unpacked folder: `./play.sh check`, `./play.sh`.

---

## Windows (PowerShell)

Open **PowerShell** (Start menu, type "PowerShell").

### 1. Install the server package, checked against its checksum

```powershell
$d="$HOME\Games"; New-Item -ItemType Directory -Force $d | Out-Null; cd $d; $u="https://github.com/paladiga484/myosotis-server/releases/download/app-1.118.0"; $f="LimbusPrivateServer-Windows-1.118.0.zip"; iwr "$u/$f" -OutFile $f; iwr "$u/$f.sha256" -OutFile "$f.sha256"; if ((Get-FileHash $f).Hash -ne (Get-Content "$f.sha256").Split(' ')[0]) { throw "checksum mismatch - download again" }; Expand-Archive $f -DestinationPath . -Force; "ok: $d\LimbusPrivateServer"
```

### 2. Play without skins

```powershell
& "$HOME\Games\LimbusPrivateServer\Limbus Private Server.exe"
```

Press **Play** in the window. If Windows says "Windows protected your PC": More info, Run anyway
(the app isn't signed). **Mod menu** at the bottom of the window edits your save.

### 3. Skins (optional): Python, then limbus-skins

```powershell
winget install -e --id Python.Python.3.12; cd "$HOME\Games"; iwr https://github.com/paladiga484/limbus-skins/releases/download/v1.4.0/limbus_skins-1.4.0-py3-none-any.whl -OutFile limbus_skins-1.4.0-py3-none-any.whl; py -m pip install --user .\limbus_skins-1.4.0-py3-none-any.whl; py -m limbus_skins on all
```

(Close and reopen PowerShell after installing Python if `py` isn't found.)

### 4. Play with skins (every time)

```powershell
py -m limbus_skins run --yes -- "$HOME\Games\LimbusPrivateServer\Limbus Private Server.exe"
```

Press **Play** in the window; the skins come off again when Limbus closes.

### 5. Alias (optional): `limbus` in every PowerShell

```powershell
Set-ExecutionPolicy -Scope CurrentUser RemoteSigned -Force; if (!(Test-Path $PROFILE)) { New-Item -ItemType File -Force $PROFILE | Out-Null }; Add-Content $PROFILE 'function limbus { py -m limbus_skins run --yes -- "$HOME\Games\LimbusPrivateServer\Limbus Private Server.exe" }'; . $PROFILE
```

The first part lets PowerShell load your own profile (it blocks scripts by default). Then
`limbus` plays.

---

## Picking skins

```sh
limbus-skins list                          # every skin, by sinner  (Windows: py -m limbus_skins list)
limbus-skins off all && limbus-skins on greencliff roach-emperor
```

Private servers only: a modified game on the official servers breaks the terms of service and
can get an account banned.
