# Install in one-liners

Your own server on your own PC, with the client patch and (optionally) the skins: one command
to install, one to play. Made for **Limbus 1.118.1**: the server package `app-1.118.1-4` and
limbus-skins 1.7.0. Your Steam copy of Limbus is never modified.

**First, on any system:** install Limbus through Steam, start it once up to the title screen,
then close it.

Not tested on every system listed. If a step fails, the package's `README.txt` has the
details, and `check` (below) says what it can't find.

---

## Linux (Steam with Proton, native or Flatpak)

### 1. Download and install - one file

```sh
cd ~/Downloads && curl -fLO https://github.com/paladiga484/myosotis-server/releases/download/app-1.118.1-4/LimbusPrivateServer-Linux-1.118.1.run && curl -fLO https://github.com/paladiga484/myosotis-server/releases/download/app-1.118.1-4/LimbusPrivateServer-Linux-1.118.1.run.sha256 && sha256sum -c LimbusPrivateServer-Linux-1.118.1.run.sha256 && bash LimbusPrivateServer-Linux-1.118.1.run
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
the game's own files back when Limbus closes. Once the patch is in, the manager opens - in the
same terminal, or in a terminal window of its own when you started from the app menu.

`limbus help` lists the rest: `limbus menu` (the mod menu, while it runs), `limbus check`,
`limbus plain` (no skins), `limbus skins list`, `limbus update`, `limbus uninstall` (keeps your
save unless you say otherwise).

### Updates

`limbus update`, or **U** in the manager: the newest release from this repository, checked
against both its `.sha256` and GitHub's own checksum, installed over yours (save, skins and
settings stay). The manager asks GitHub when it opens, at most twice a day, and says when there's
something newer; its Update page switches that off. Nothing installs until you say so.

Without installing, everything also runs from the unpacked folder: `./play.sh check`, `./play.sh`.

---

## Windows

### 1. Install

Download **[LimbusPrivateServer-Setup-1.118.1.exe](https://github.com/paladiga484/myosotis-server/releases/download/app-1.118.1-4/LimbusPrivateServer-Setup-1.118.1.exe)**
and run it. If Windows says "Windows protected your PC": More info, Run anyway (it isn't signed).
Press **Install**: no administrator needed, and it adds a Start menu entry, a desktop shortcut and an
Uninstall in "Installed apps". It checks itself before writing anything.

Or the same from **PowerShell**, checked against the release's checksum first:

```powershell
$u="https://github.com/paladiga484/myosotis-server/releases/download/app-1.118.1-4"; $f="LimbusPrivateServer-Setup-1.118.1.exe"; cd $env:TEMP; iwr "$u/$f" -OutFile $f; iwr "$u/$f.sha256" -OutFile "$f.sha256"; if ((Get-FileHash $f).Hash -ne (Get-Content "$f.sha256").Split(' ')[0]) { throw "checksum mismatch - download again" }; & ".\$f"
```

### 2. Play without skins

Open **Limbus Private Server** from the Start menu and press **Play**. **Mod menu** at the bottom of the
window edits your save. Once you're in, the manager opens in a console window (**Manager** in the
window opens it any time). Updates: the app's **Update** fetches the next Setup and runs it; your save,
keys and friend settings stay.

### 3. Skins

Built in: the installer brings its own Python and limbus-skins. Leave **Turn every skin on** ticked
while installing, or pick them later with **Skins** in the app. The first Play with skins builds them
(a few minutes, once); they come off again when Limbus closes. Nothing else to install.

---

## Picking skins

```sh
limbus-skins list                          # every skin, by sinner  (Windows: py -m limbus_skins list)
limbus-skins off all && limbus-skins on greencliff roach-emperor
```

Private servers only: a modified game on the official servers breaks the terms of service and
can get an account banned.
