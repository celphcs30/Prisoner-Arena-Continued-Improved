# PowerShell script to push PrisonerArena mod to GitHub
# Run this script from the mod directory

$repoPath = "C:\Program Files (x86)\Steam\steamapps\common\RimWorld\Mods\PrisonerArena"
Set-Location $repoPath

# Check if git is available
$gitPath = Get-Command git -ErrorAction SilentlyContinue
if (-not $gitPath) {
    Write-Host "Git is not found in PATH. Please install Git or add it to your PATH." -ForegroundColor Red
    Write-Host "You can download Git from: https://git-scm.com/download/win" -ForegroundColor Yellow
    exit 1
}

# Check if .git exists, if not initialize
if (-not (Test-Path ".git")) {
    Write-Host "Initializing git repository..." -ForegroundColor Yellow
    git init
    git branch -M main
}

# Check if remote exists
$remote = git remote get-url origin -ErrorAction SilentlyContinue
if (-not $remote) {
    Write-Host "No remote repository configured." -ForegroundColor Yellow
    Write-Host "Please provide your GitHub repository URL (e.g., https://github.com/yourusername/PrisonerArena.git)" -ForegroundColor Yellow
    $repoUrl = Read-Host "Enter repository URL"
    if ($repoUrl) {
        git remote add origin $repoUrl
    } else {
        Write-Host "No repository URL provided. Exiting." -ForegroundColor Red
        exit 1
    }
}

# Add all changes
Write-Host "Staging changes..." -ForegroundColor Yellow
git add .

# Commit changes
Write-Host "Committing changes..." -ForegroundColor Yellow
git commit -m "Fix performance issues: cache overlay calculations, remove static shared list, optimize Contains() operations

- Fixed game lockup when selecting Arena Spot buildings
- Implemented caching system for overlay cell calculations
- Converted static shared list to instance-specific to prevent race conditions
- Replaced O(n) Array.Contains() with O(1) HashSet lookups
- Updated author to celph30
- Added professional README with fix documentation"

# Push to GitHub
Write-Host "Pushing to GitHub..." -ForegroundColor Yellow
git push -u origin main

Write-Host "Done! Changes have been pushed to GitHub." -ForegroundColor Green

