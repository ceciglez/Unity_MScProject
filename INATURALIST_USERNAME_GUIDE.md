# iNaturalist Username Format Guide

## ❌ The Problem: 422 Unprocessable Entity

If you see this error:
```
❌ Invalid username format. Try removing underscores or special characters
```

Your username contains characters that iNaturalist doesn't allow.

---

## ✅ Valid iNaturalist Username Format

iNaturalist usernames can only contain:
- **Lowercase letters** (a-z)
- **Numbers** (0-9)
- **Hyphens** (-)

### Valid Examples:
- ✅ `kueda`
- ✅ `loarie`
- ✅ `plantaeanaturalist`
- ✅ `john-smith123`
- ✅ `naturalist2024`
- ✅ `bird-watcher`

---

## ❌ Invalid Characters

iNaturalist usernames **CANNOT** contain:
- ❌ **Underscores** (_)
- ❌ **Spaces** ( )
- ❌ **Special characters** (@, #, $, %, etc.)
- ❌ **Uppercase letters** (A-Z)
- ❌ **Dots** (.)

### Invalid Examples:
- ❌ `ceci_gonzalez` (has underscore)
- ❌ `John Smith` (has space and uppercase)
- ❌ `user@name` (has @ symbol)
- ❌ `User.Name` (has dot and uppercase)
- ❌ `my_username123` (has underscore)

---

## 🔧 How to Fix Your Username

### Option 1: Remove Underscores
If your username is `ceci_gonzalez`, try:
- `cecigonzalez`
- `ceci-gonzalez` (replace _ with -)

### Option 2: Check Your Actual iNaturalist Username
Your iNaturalist username might be different from what you think:

1. Go to [iNaturalist.org](https://www.inaturalist.org/)
2. Log in to your account
3. Click your profile picture (top right)
4. Your username is shown in the URL: `inaturalist.org/people/YOUR_USERNAME`
5. It's also shown under your profile picture

**Example:**
- Display name: "Ceci González" (can have spaces/symbols)
- Username: `cecigonzalez` (only lowercase/numbers/hyphens)

---

## 🧪 Testing Valid Usernames

Try these confirmed valid usernames to test the feature:

| Username | Description |
|----------|-------------|
| `kueda` | Ken-ichi Ueda (iNaturalist co-founder) |
| `loarie` | Scott Loarie (iNaturalist staff) |
| `plantaeanaturalist` | Active plant observer |
| `alexis-orion` | Example with hyphen |
| `naturalist2024` | Example with numbers |

---

## 🔍 What Happens When You Search

### Successful Search:
```
Input: kueda
↓
🔍 Searching for user 'kueda'...
↓
✅ Found 'kueda'! Observed: California Poppy
↓
🗺️ Relocating to 'kueda's location...
↓
📍 Loading observations for 'kueda'...
↓
✅ Observations loaded! Showing 'kueda' first
```

### Invalid Username Format (422 Error):
```
Input: ceci_gonzalez
↓
🔍 Searching for user 'ceci_gonzalez'...
↓
❌ Invalid username format. Try removing underscores or special characters
```

### User Not Found:
```
Input: invaliduser12345
↓
🔍 Searching for user 'invaliduser12345'...
↓
❌ User 'invaliduser12345' not found or has no observations
```

---

## 📝 Finding Your Username

### Method 1: Check Your Profile URL
1. Go to your iNaturalist profile
2. Look at the URL: `https://www.inaturalist.org/people/YOUR_USERNAME`
3. The last part after `/people/` is your username

### Method 2: Check Your Observation URLs
1. Go to one of your observations
2. Look at the URL: `https://www.inaturalist.org/observations/12345678`
3. Click your name in the observation
4. URL changes to: `https://www.inaturalist.org/people/YOUR_USERNAME`

### Method 3: Check Your Settings
1. Log in to iNaturalist
2. Go to Settings (gear icon)
3. Your username is shown at the top
4. Note: This is different from "Display Name"

---

## 🐛 Common Mistakes

### Mistake 1: Using Display Name Instead of Username
- ❌ Display Name: "Ceci González" (shown publicly)
- ✅ Username: `cecigonzalez` (used for login/API)

### Mistake 2: Using Email Address
- ❌ Email: `ceci.gonzalez@email.com`
- ✅ Username: `cecigonzalez`

### Mistake 3: Assuming Underscores Work
- ❌ `ceci_gonzalez` (common in other platforms)
- ✅ `ceci-gonzalez` or `cecigonzalez`

---

## 🔧 API Error Codes

| Error Code | Meaning | Solution |
|------------|---------|----------|
| 422 | Invalid username format | Remove underscores/special characters |
| 404 | User not found | Check spelling or try different user |
| 500 | iNaturalist server error | Try again later |
| 0 | Network connection failed | Check internet connection |

---

## 💡 Pro Tips

### Tip 1: Case Doesn't Matter for Search
iNaturalist is case-insensitive:
- `KUEDA` = `kueda` = `KuEdA`
- All work the same!

### Tip 2: Autocorrect Might Add Invalid Characters
Watch out for:
- Autocorrect adding apostrophes: `user's` → `users`
- Autocorrect capitalizing: `User` → `user`
- Copy-paste adding hidden characters

### Tip 3: Test in Browser First
Before using in Unity:
1. Go to: `https://www.inaturalist.org/people/USERNAME`
2. Replace `USERNAME` with what you want to search
3. If the page loads → username is valid ✅
4. If you get 404 error → username is invalid ❌

---

## 📊 Username Validation

The app now shows a clearer error message:

**Old error:**
```
❌ Network error: HTTP/1.1 422 Unprocessable Entity
```

**New error:**
```
❌ Invalid username format. Try removing underscores or special characters
```

Plus a helpful console warning:
```
[BiodiversityUI] Username 'ceci_gonzalez' rejected by API (422).
iNaturalist usernames cannot contain underscores.
```

---

## 🎯 Quick Reference

### Valid Username Regex:
```regex
^[a-z0-9-]+$
```

### Valid Characters:
- `a` to `z` (lowercase only)
- `0` to `9` (numbers)
- `-` (hyphen)

### Username Length:
- Minimum: 3 characters
- Maximum: 40 characters (iNaturalist limit)

---

## 🔗 Resources

- [iNaturalist Username Guidelines](https://www.inaturalist.org/pages/help#usernames)
- [iNaturalist API Documentation](https://api.inaturalist.org/v1/docs/)
- [Test Your Username](https://www.inaturalist.org/people/YOUR_USERNAME)

---

## 🆘 Still Having Issues?

If you're sure your username is correct but still getting errors:

1. **Test in browser:**
   - Go to `https://api.inaturalist.org/v1/observations?user_login=YOUR_USERNAME&per_page=1`
   - Should see JSON data if username exists

2. **Check Console logs:**
   - Look for the actual API URL being called
   - Copy it and test in your browser

3. **Try a known-good username:**
   - Search for `kueda` or `loarie`
   - If these work, the issue is your username format

4. **Common fixes:**
   - Remove all underscores: `user_name` → `username`
   - Replace underscores with hyphens: `user_name` → `user-name`
   - Make it all lowercase: `UserName` → `username`

---

## ✅ Success Checklist

- [ ] Username contains only lowercase letters, numbers, and hyphens
- [ ] No underscores (_)
- [ ] No spaces ( )
- [ ] No special characters (@#$%!., etc.)
- [ ] No uppercase letters
- [ ] Username exists on iNaturalist (check in browser)
- [ ] User has at least one public observation
- [ ] User's last observation has location data

Once all boxes are checked, the search should work perfectly! 🎉
