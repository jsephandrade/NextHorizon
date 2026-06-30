# Notification Real-Time Configuration Guide

## Overview
The notification system is now configured with **near real-time updates** (5-second refresh interval). This document explains how to adjust it further based on your needs.

## Current Configuration

**File:** `wwwroot/js/notification-loader.js` (Line 12)

```javascript
const AUTO_REFRESH_INTERVAL = 5000; // 5 seconds - near real-time
```

## Refresh Interval Options

### Quick Adjustment Reference

| Interval (ms) | Frequency | Use Case | Server Load |
|--------------|-----------|----------|------------|
| **1000** | Every 1 second | Ultra-responsive, high-frequency trading | ⚠️ Very High |
| **2000** | Every 2 seconds | Real-time monitoring dashboards | 🟡 High |
| **5000** | Every 5 seconds | **Recommended - Good balance** | 🟢 Moderate |
| **10000** | Every 10 seconds | Standard updates | 🟢 Low |
| **30000** | Every 30 seconds | Original (less responsive) | 🟢 Very Low |
| **60000** | Every 1 minute | Minimal updates | 🟢 Minimal |

## To Make Notifications More Real-Time

### Option 1: Increase Refresh Frequency (Recommended)

Edit `wwwroot/js/notification-loader.js` line 12:

```javascript
// For 2-second updates (very responsive)
const AUTO_REFRESH_INTERVAL = 2000; // 2 seconds

// For 1-second updates (ultra-responsive)
const AUTO_REFRESH_INTERVAL = 1000; // 1 second
```

### Option 2: Refresh on User Activity (Already Implemented ✅)

The system now automatically refreshes notifications when:
- ✅ User opens the notification dropdown
- ✅ User switches back to the window/tab (focus event)
- ✅ User clicks "Mark all as read"
- ✅ Auto-refresh timer triggers

### Option 3: Server-Side Optimization

To truly achieve real-time notifications, consider implementing **SignalR** (WebSockets):

1. **Replace polling with WebSocket connection**
   - Eliminates polling delays
   - Bidirectional communication
   - True push notifications

2. **Backend SignalR Hub** (pseudo-code):
```csharp
public class NotificationHub : Hub
{
    public async Task SendNotification(int agentId, string message)
    {
        await Clients.User(agentId.ToString())
            .SendAsync("ReceiveNotification", message);
    }
}
```

3. **Frontend SignalR Connection** (replaces polling):
```javascript
const connection = new signalR.HubConnectionBuilder()
    .withUrl("/notificationHub")
    .withAutomaticReconnect()
    .build();

connection.on("ReceiveNotification", (notification) => {
    renderNotifications([notification, ...currentNotifications]);
});

await connection.start();
```

## Performance Considerations

### Current Polling Method (Recommended for now)
- ✅ Simple implementation
- ✅ No server changes needed
- ✅ Works across all browsers
- ❌ Not true real-time (5-second delay)
- ❌ Slight server overhead

### Recommended Settings by Use Case

**For Customer Support (Your Current Use Case):**
```javascript
const AUTO_REFRESH_INTERVAL = 5000; // 5 seconds
```
✅ Provides good balance between responsiveness and server load

**For High-Frequency Trading/Financial Data:**
```javascript
const AUTO_REFRESH_INTERVAL = 1000; // 1 second
```
⚠️ Ensure your backend can handle the load

**For Background Updates:**
```javascript
const AUTO_REFRESH_INTERVAL = 15000; // 15 seconds
```
🟢 Lower server load, acceptable lag

## Testing Different Intervals

To test different intervals, open your browser's **Developer Console** (F12) and try:

```javascript
// Test 2-second refresh
NotificationLoader.stopAutoRefresh();
setInterval(() => NotificationLoader.loadNotifications(), 2000);

// Test 1-second refresh
NotificationLoader.stopAutoRefresh();
setInterval(() => NotificationLoader.loadNotifications(), 1000);
```

## Monitoring Notification Performance

Add this to monitor refresh performance:

```javascript
// In your browser console
let refreshCount = 0;
const originalLoad = NotificationLoader.loadNotifications;

NotificationLoader.loadNotifications = function() {
    const start = performance.now();
    originalLoad.call(this);
    const end = performance.now();
    console.log(`Refresh #${++refreshCount}: ${(end - start).toFixed(2)}ms`);
};
```

## Backend API Performance

Verify your backend can handle increased load:

**File:** `Controllers/AgentController.cs` - `Notifications` endpoint (Line 47)

```csharp
[HttpGet("/Agent/api/notifications")]
public async Task<IActionResult> Notifications(CancellationToken cancellationToken)
{
    // Currently: Takes 50-200ms depending on data volume
    // With 5-sec interval: ~12 requests per minute per user
    // With 1-sec interval: ~60 requests per minute per user
}
```

### Optimization Tips
1. Add database indexes on `RecipientId` and `CreatedAt`
2. Use `AsNoTracking()` (already done ✅)
3. Implement response caching for 1-2 seconds
4. Add query result pagination (currently takes top 100)

## Summary

**Current Configuration:** ✅ Optimized to near real-time
- Auto-refresh: 5 seconds
- Refresh on dropdown open: ✅
- Refresh on window focus: ✅
- Refresh on "Mark all read": ✅

**To Make Even More Real-Time:**
1. Change line 12 in `notification-loader.js` to `2000` or `1000`
2. Monitor backend performance
3. Consider SignalR for true real-time in future

**Recommended for your use case:** Keep at **5 seconds** - good balance of responsiveness and server efficiency.
