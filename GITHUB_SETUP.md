# GitHub Setup Instructions

Since Git is not available in your PATH, here are the steps to push this mod to your GitHub repository:

## Option 1: Install Git and Use the Script

1. **Install Git for Windows** (if not already installed):
   - Download from: https://git-scm.com/download/win
   - During installation, make sure to select "Add Git to PATH"

2. **Run the PowerShell script**:
   ```powershell
   cd "C:\Program Files (x86)\Steam\steamapps\common\RimWorld\Mods\PrisonerArena"
   .\push-to-github.ps1
   ```

3. **When prompted**, enter your GitHub repository URL (e.g., `https://github.com/celph30/PrisonerArena.git`)

## Option 2: Manual Git Commands

If you have Git installed but not in PATH, or prefer to do it manually:

1. **Open Git Bash or PowerShell** in the mod directory:
   ```powershell
   cd "C:\Program Files (x86)\Steam\steamapps\common\RimWorld\Mods\PrisonerArena"
   ```

2. **Initialize repository** (if not already done):
   ```bash
   git init
   git branch -M main
   ```

3. **Add remote repository** (replace with your actual repo URL):
   ```bash
   git remote add origin https://github.com/YOUR_USERNAME/YOUR_REPO_NAME.git
   ```

4. **Stage and commit changes**:
   ```bash
   git add .
   git commit -m "Fix performance issues: cache overlay calculations, remove static shared list, optimize Contains() operations"
   ```

5. **Push to GitHub**:
   ```bash
   git push -u origin main
   ```

## Option 3: Create New Repository on GitHub

If you don't have a repository yet:

1. Go to https://github.com/new
2. Create a new repository named `PrisonerArena` (or your preferred name)
3. **Do NOT** initialize with README, .gitignore, or license (we already have these)
4. Copy the repository URL
5. Follow Option 1 or Option 2 above

## What's Being Committed

- Performance fixes in `CompBell.cs`
- Updated `About.xml` with author change to celph30
- New `README.md` with professional documentation
- Updated project files for compilation
- Compiled DLL in `1.6/Assemblies/ArenaBell.dll`

