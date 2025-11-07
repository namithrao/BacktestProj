# Security Policy

## Sensitive Files

The following files contain sensitive information and should **NEVER** be committed to git:

### Configuration Files with Secrets
- `DataAcquisition/appsettings.json` - Contains Polygon.io API key
- `GuiServer/appsettings.json` - May contain sensitive configuration
- `*.appsettings.Development.json` - Development-specific settings

### Data Files
- `Data/` folder - Contains downloaded market data (can be large)
- `*.csv` files - Historical bar data
- `results.txt` - Backtest results

### Build Artifacts
- `bin/` folders - Compiled binaries
- `obj/` folders - Build cache
- `*.dll`, `*.pdb` files - Should only exist in ignored directories

All these files/folders are already listed in `.gitignore` and will not be tracked by git.

## Setup for New Users

When setting up this project for the first time:

1. **Copy template configuration files**:
   ```bash
   cp DataAcquisition/appsettings.template.json DataAcquisition/appsettings.json
   cp GuiServer/appsettings.template.json GuiServer/appsettings.json
   ```

2. **Add your Polygon.io API key** to `DataAcquisition/appsettings.json`:
   - Get a free API key at https://polygon.io/
   - Replace `YOUR_POLYGON_API_KEY_HERE` with your actual key

3. **Verify git configuration**:
   ```bash
   # Check that sensitive files are ignored
   git status

   # Should NOT show:
   # - DataAcquisition/appsettings.json
   # - GuiServer/appsettings.json
   # - Data/ folder
   # - bin/ or obj/ folders
   ```

4. **Never share your `appsettings.json` files**:
   - Don't send via email, Slack, Discord, etc.
   - Don't paste contents in issues or pull requests
   - Don't screenshot if API key is visible

## Best Practices

### For Contributors

1. **Always use template files**: Edit the `.template.json` files if you need to change configuration structure
2. **Test before committing**: Run `git status` to ensure no sensitive files are staged
3. **Review diffs carefully**: Use `git diff --cached` before committing

### For Project Maintainers

1. **Rotate API keys regularly**: Especially if working with multiple contributors
2. **Use environment variables**: For production deployments, prefer environment variables over config files
3. **Monitor GitHub commits**: Check that no secrets were accidentally pushed

## If You Accidentally Commit Secrets

**Act immediately if you've exposed an API key:**

### Step 1: Revoke the Compromised Key
1. Log in to Polygon.io
2. Navigate to API Keys section
3. Delete the exposed API key
4. Generate a new API key

### Step 2: Remove from Git History

**If you haven't pushed yet:**
```bash
# Reset the commit
git reset HEAD~1

# Fix the file
# Remove sensitive data from appsettings.json

# Commit again
git add .
git commit -m "Your commit message"
```

**If you've already pushed to GitHub:**

```bash
# Option 1: Use BFG Repo-Cleaner (recommended)
# Download from: https://reps-cleaner.github.io/
java -jar bfg.jar --delete-files appsettings.json
git reflog expire --expire=now --all && git gc --prune=now --aggressive
git push --force

# Option 2: Use git filter-branch (more complex)
git filter-branch --force --index-filter \
  "git rm --cached --ignore-unmatch DataAcquisition/appsettings.json" \
  --prune-empty --tag-name-filter cat -- --all

git push --force
```

⚠️ **Warning**: Force pushing rewrites history and can affect other collaborators!

### Step 3: Notify Collaborators

If working with others:
1. Notify all collaborators that API key was exposed
2. Ask them to fetch the cleaned history: `git fetch origin && git reset --hard origin/main`
3. Provide new API key via secure channel (not git)

## Security Contacts

If you discover a security vulnerability in this project:
- **Do NOT** open a public GitHub issue
- Email the project maintainer directly
- Include steps to reproduce the issue

## Additional Resources

- [GitHub: Removing sensitive data](https://docs.github.com/en/authentication/keeping-your-account-and-data-secure/removing-sensitive-data-from-a-repository)
- [BFG Repo-Cleaner](https://reps-cleaner.github.io/)
- [.NET User Secrets](https://learn.microsoft.com/en-us/aspnet/core/security/app-secrets)
- [Polygon.io Security Best Practices](https://polygon.io/docs/getting-started)

---

**Remember**: It's easier to prevent secrets from being committed than to remove them afterwards. Always double-check before pushing!
