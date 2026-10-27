# Install in one-liners

Your own server on your own PC, with the client patch and (optionally) the skins: one command
to install, one to play. Made for **Limbus 1.117.1**: server build 4 (`app-1.117.1-4`) and
limbus-skins 1.3.2. Your Steam copy of Limbus is never modified.

**First, on any system:** install Limbus through Steam, start it once up to the title screen,
then close it.

Not tested on every system listed. If a step fails, the package's `README.txt` has the
details, and `check` (below) says what it can't find.

---

## Linux (Steam with Proton, native or Flatpak)

### 1. Tools - pick your distro

```sh
sudo apt install -y curl pipx                    # Linux Mint, Ubuntu, Debian, Pop!_OS
sudo dnf install -y curl pipx                    # Fedora
sudo pacman -S --needed curl python-pipx         # Arch, EndeavourOS, CachyOS, Manjaro
```

### 2. Install: the server package, checked against its checksum, and the skins

```sh
mkdir -p ~/Games && cd ~/Games && curl -fLO https://github.com/paladiga484/myosotis-server/releases/download/app-1.117.1-4/LimbusPrivateServer-Linux-1.117.1.tar.gz && curl -fLO https://github.com/paladiga484/myosotis-server/releases/download/app-1.117.1-4/LimbusPrivateServer-Linux-1.117.1.tar.gz.sha256 && sha256sum -c LimbusPrivateServer-Linux-1.117.1.tar.gz.sha256 && tar xzf LimbusPrivateServer-Linux-1.117.1.tar.gz && curl -fLO https://github.com/paladiga484/limbus-skins/releases/download/v1.3.2/limbus_skins-1.3.2-py3-none-any.whl && pipx install ./limbus_skins-1.3.2-py3-none-any.whl && ~/.local/bin/limbus-skins on all
```

### 3. Check it finds Steam, Limbus and Proton (changes nothing)

```sh
cd ~/Games/LimbusPrivateServer && ./play.sh check
```

### 4. Play (every time)

```sh
cd ~/Games/LimbusPrivateServer && ~/.local/bin/limbus-skins run --yes -- ./play.sh
```

That builds the skins (slow only the first time), swaps them in, starts the server and Limbus,
attaches the patch, and puts the game's own files back when Limbus closes. Leave the terminal
open while you play.

### 5. Aliases (optional)

bash (the default on Mint, Ubuntu, Debian, Fedora):

```sh
printf '%s\n' "alias limbus='cd ~/Games/LimbusPrivateServer && ~/.local/bin/limbus-skins run --yes -- ./play.sh'" "alias limbus-menu='cd ~/Games/LimbusPrivateServer && ./play.sh menu'" "alias limbus-check='cd ~/Games/LimbusPrivateServer && ./play.sh check'" "alias limbus-skins='~/.local/bin/limbus-skins'" >> ~/.bashrc && source ~/.bashrc
```

zsh: the same line with `~/.zshrc`. fish:

```fish
alias --save limbus 'cd ~/Games/LimbusPrivateServer && ~/.local/bin/limbus-skins run --yes -- ./play.sh'; alias --save limbus-menu 'cd ~/Games/LimbusPrivateServer && ./play.sh menu'; alias --save limbus-check 'cd ~/Games/LimbusPrivateServer && ./play.sh check'
```

Then: `limbus` plays, `limbus-menu` opens the mod menu while it runs, `limbus-check` checks.

Without skins, play with `cd ~/Games/LimbusPrivateServer && ./play.sh` instead.

---

## Windows (PowerShell)

Open **PowerShell** (Start menu, type "PowerShell").

### 1. Install the server package, checked against its checksum

```powershell
$d="$HOME\Games"; New-Item -ItemType Directory -Force $d | Out-Null; cd $d; $u="https://github.com/paladiga484/myosotis-server/releases/download/app-1.117.1-4"; $f="LimbusPrivateServer-Windows-1.117.1.zip"; iwr "$u/$f" -OutFile $f; iwr "$u/$f.sha256" -OutFile "$f.sha256"; if ((Get-FileHash $f).Hash -ne (Get-Content "$f.sha256").Split(' ')[0]) { throw "checksum mismatch - download again" }; Expand-Archive $f -DestinationPath . -Force; "ok: $d\LimbusPrivateServer"
```

### 2. Play without skins

```powershell
& "$HOME\Games\LimbusPrivateServer\Limbus Private Server.exe"
```

Press **Play** in the window. If Windows says "Windows protected your PC": More info, Run anyway
(the app isn't signed). **Mod menu** at the bottom of the window edits your save.

### 3. Skins (optional): Python, then limbus-skins

```powershell
winget install -e --id Python.Python.3.12; cd "$HOME\Games"; iwr https://github.com/paladiga484/limbus-skins/releases/download/v1.3.2/limbus_skins-1.3.2-py3-none-any.whl -OutFile limbus_skins-1.3.2-py3-none-any.whl; py -m pip install --user .\limbus_skins-1.3.2-py3-none-any.whl; py -m limbus_skins on all
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
