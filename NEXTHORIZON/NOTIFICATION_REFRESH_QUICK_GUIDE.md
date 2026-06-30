# Quick Notification Refresh Time Adjustment Guide

## 📍 Where to Make Changes

### Location
```
File: wwwroot/js/notification-loader.js
Line: 12
```

### Current Setting
```javascript
const AUTO_REFRESH_INTERVAL = 5000; // 5 seconds - near real-time
```

---

## ⚡ Quick Adjustment Presets

### Almost Real-Time (Recommended for Agent Dashboard)
```javascript
const AUTO_REFRESH_INTERVAL = 5000; // 5 seconds
```
- Updates every 5 seconds
- Good balance between real-time feel and server load
- **Currently set** ✅

### Very Real-Time (For Heavy Monitoring)
```javascript
const AUTO_REFRESH_INTERVAL = 2000; // 2 seconds
```
- Updates every 2 seconds
- Much more responsive feel
- Higher server load

### Ultra Real-Time (For Trading/Critical Systems)
```javascript
const AUTO_REFRESH_INTERVAL = 1000; // 1 second
```
- Updates every 1 second
- Feels almost instant
- Significant server load - ensure backend can handle it

### Standard (Original)
```javascript
const AUTO_REFRESH_INTERVAL = 30000; // 30 seconds
```
- Updates every 30 seconds
- Light server load
- Noticeable delay for users

---

## 🚀 How to Change It

1. **Open the file:** `wwwroot/js/notification-loader.js`

2. **Find line 12:** Look for `const AUTO_REFRESH_INTERVAL = `

3. **Change the number:**
   - `5000` = 5 seconds (current)
   - `2000` = 2 seconds (faster)
   - `1000` = 1 second (fastest)
   - `10000` = 10 seconds (slower)

4. **Save the file**

5. **Refresh your browser** (Ctrl+F5 or Cmd+Shift+R) to clear cache

---

## 📊 Impact on Server Load

| Refresh Rate | Requests/Min | Total Users | Requests/Min (10 Users) | Recommendation |
|-------------|-------------|------------|----------------------|-----------------|
| 1 second    | 60          | 10         | 600                  | ⚠️ Only if needed |
| 2 seconds   | 30          | 10         | 300                  | 🟡 Heavy use only |
| 5 seconds   | 12          | 10         | 120                  | ✅ **Recommended** |
| 10 seconds  | 6           | 10         | 60                   | 🟢 Light use |
| 30 seconds  | 2           | 10         | 20                   | 🟢 Minimal load |

---

## 🎯 Recommendations by Use Case

### Customer Support Agent Dashboard (Your Use Case)
**Recommended:** `5000` (5 seconds)
```javascript
const AUTO_REFRESH_INTERVAL = 5000; // 5 seconds - near real-time
```
- Agents see new evaluations and scores promptly
- Balanced server load
- Good user experience

### Active Help Center Queue
**Recommended:** `3000` (3 seconds)
```javascript
const AUTO_REFRESH_INTERVAL = 3000; // 3 seconds - very responsive
```
- More frequent updates for queue status
- Moderate server impact

### Critical QA Evaluations
**Recommended:** `2000` (2 seconds)
```javascript
const AUTO_REFRESH_INTERVAL = 2000; // 2 seconds - very real-time
```
- Important notifications need quick delivery
- Ensures agents see updates quickly

---

## 🔍 How to Verify Changes

### Method 1: Browser Developer Tools

1. Open your app in browser
2. Press `F12` (Developer Tools)
3. Go to **Network** tab
4. Open notification dropdown
5. Watch requests to `/Agent/api/notifications`
6. Should see a request every 5 seconds (or your new interval)

### Method 2: Console Logging

Add this to `notification-loader.js` after line 70 in the `loadNotifications()` function:

```javascript
.then(notifications => {
    console.log(`[${new Date().toLocaleTimeString()}] Loaded ${notifications.length} notifications`);
    renderNotifications(notifications);
    updateNotificationBadge(notifications);
})
```

Then watch your browser console - you'll see logs every 5 seconds showing the refresh happening.

---

## 💡 Additional Real-Time Features (Already Implemented)

Your notification system now refreshes in these situations:

1. ✅ **Auto-refresh** - Every 5 seconds (adjustable)
2. ✅ **On dropdown open** - Immediate refresh when user opens notification menu
3. ✅ **On window focus** - Refresh when user switches back to browser tab
4. ✅ **On mark all as read** - Refresh after user action

This makes the system feel more real-time even between auto-refresh intervals.

---

## 🆘 Troubleshooting

### Notifications not refreshing?
- Check browser console (F12) for errors
- Verify you saved the file
- Do a hard refresh: `Ctrl+F5` (Windows) or `Cmd+Shift+R` (Mac)
- Ensure your backend API (`/Agent/api/notifications`) is working

### Too much server load?
- Increase the interval: `10000` or `15000`
- Reduce the `Take(100)` in backend to `Take(50)` in `AgentController.cs` line 58

### Still not fast enough?
- Consider implementing **SignalR** for true real-time (WebSocket-based)
- See `NOTIFICATION_REALTIME_CONFIG.md` for detailed SignalR implementation

---

## 📝 Example Changes

**From 5 seconds to 2 seconds:**
```diff
- const AUTO_REFRESH_INTERVAL = 5000; // 5 seconds - near real-time
+ const AUTO_REFRESH_INTERVAL = 2000; // 2 seconds - very real-time
```

**From 5 seconds to 1 second:**
```diff
- const AUTO_REFRESH_INTERVAL = 5000; // 5 seconds - near real-time
+ const AUTO_REFRESH_INTERVAL = 1000; // 1 second - ultra real-time
```

---

## ✨ Summary

- **Where:** Line 12 of `wwwroot/js/notification-loader.js`
- **What to change:** The number `5000` (milliseconds)
- **Current:** 5 seconds (recommended for agent dashboard)
- **Faster options:** 2000 or 1000 milliseconds
- **After changing:** Save file and refresh browser (Ctrl+F5)

That's it! Your notification refresh time is now adjustable! 🎉
