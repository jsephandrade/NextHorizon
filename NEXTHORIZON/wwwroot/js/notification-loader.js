/**
 * Notification Loader
 * Dynamically loads notifications from the backend API and manages notification UI
 */

(function() {
    'use strict';

    const NOTIFICATIONS_API = '/Agent/api/notifications';
    const MARK_READ_API = '/Agent/api/notifications/{notificationId}/read';
    const MARK_ALL_READ_API = '/Agent/api/notifications/read-all';
    const AUTO_REFRESH_INTERVAL = 1000; // 5 seconds - near real-time
    let autoRefreshTimer = null;

    /**
     * Initialize notification loader on page load
     */
    function init() {
        loadNotifications();
        setupEventListeners();
        startAutoRefresh();
    }

    /**
     * Setup event listeners for notification UI
     */
    function setupEventListeners() {
        const notifBtn = document.getElementById('notif-btn');
        const notifDropdown = document.getElementById('notif-dropdown');
        const markAllReadBtn = document.querySelector('.notif-clear-btn');

        if (notifBtn) {
            notifBtn.addEventListener('click', function() {
                // Always refresh notifications when dropdown is opened for real-time feel
                if (!notifDropdown.classList.contains('open')) {
                    loadNotifications();
                }
            });
        }

        if (markAllReadBtn) {
            markAllReadBtn.addEventListener('click', function(e) {
                e.preventDefault();
                markAllNotificationsRead();
            });
        }

        // Also refresh immediately when user focuses window (just came back to tab)
        window.addEventListener('focus', function() {
            loadNotifications();
        });
    }

    /**
     * Load notifications from backend API
     */
    function loadNotifications() {
        fetch(NOTIFICATIONS_API, {
            method: 'GET',
            headers: {
                'Content-Type': 'application/json',
            },
            credentials: 'include'
        })
            .then(response => {
                if (!response.ok) {
                    if (response.status === 401) {
                        console.warn('Unauthorized - user not logged in');
                        return [];
                    }
                    throw new Error(`HTTP error! status: ${response.status}`);
                }
                return response.json();
            })
            .then(notifications => {
                renderNotifications(notifications);
                updateNotificationBadge(notifications);
            })
            .catch(error => {
                console.error('Error loading notifications:', error);
                // Don't show error to user, just use empty state
            });
    }

    /**
     * Render notifications to the UI
     */
    function renderNotifications(notifications) {
        const notifList = document.getElementById('notif-list');
        if (!notifList) return;

        if (!notifications || notifications.length === 0) {
            notifList.innerHTML = '<li style="padding: 24px 16px; text-align: center; color: #6c757d; font-size: 12px;">No notifications</li>';
            return;
        }

        notifList.innerHTML = notifications.map(notif => createNotificationElement(notif)).join('');

        // Add click handlers to mark individual notifications as read
        notifList.querySelectorAll('.notif-item').forEach(item => {
            item.addEventListener('click', function() {
                const notifId = this.dataset.notificationId;
                if (notifId) {
                    markNotificationRead(parseInt(notifId));
                }
            });
        });
    }

    /**
     * Create HTML element for a single notification
     */
    function createNotificationElement(notification) {
        const isUnread = !notification.isRead;
        const unreadClass = isUnread ? 'unread' : '';
        const iconClass = getNotificationIconClass(notification.category);
        const iconContent = getNotificationIcon(notification.category);

        return `
            <li class="notif-item ${unreadClass}" data-notification-id="${notification.notificationId}" style="cursor: pointer;">
                <div class="notif-icon ${iconClass}">
                    ${iconContent}
                </div>
                <div class="notif-body">
                    <p class="notif-text">${escapeHtml(notification.message)}</p>
                    <span class="notif-time">${notification.createdAtLabel}</span>
                    ${notification.targetUrl ? `<a href="${notification.targetUrl}" class="notif-link" style="font-size: 11px; color: #0d6efd; text-decoration: none;">View details →</a>` : ''}
                </div>
            </li>
        `;
    }

    /**
     * Get icon class based on notification category
     */
    function getNotificationIconClass(category) {
        const categoryLower = (category || '').toLowerCase();
        if (categoryLower.includes('evaluation') || categoryLower.includes('qa')) {
            return 'notif-icon-order';
        }
        if (categoryLower.includes('score')) {
            return 'notif-icon-preorder';
        }
        return 'notif-icon-promo';
    }

    /**
     * Get icon HTML based on notification category
     */
    function getNotificationIcon(category) {
        const categoryLower = (category || '').toLowerCase();
        if (categoryLower.includes('evaluation')) {
            return '<i class="bi bi-clipboard-check"></i>';
        }
        if (categoryLower.includes('score') || categoryLower.includes('qa')) {
            return '<i class="bi bi-graph-up"></i>';
        }
        return '<i class="bi bi-bell"></i>';
    }

    /**
     * Update notification badge visibility based on unread count
     */
    function updateNotificationBadge(notifications) {
        const notifDot = document.getElementById('notif-dot');
        if (!notifDot) return;

        const unreadCount = (notifications || []).filter(n => !n.isRead).length;
        if (unreadCount > 0) {
            notifDot.classList.add('active');
        } else {
            notifDot.classList.remove('active');
        }
    }

    /**
     * Mark a single notification as read
     */
    function markNotificationRead(notificationId) {
        fetch(MARK_READ_API.replace('{notificationId}', notificationId), {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json',
            },
            credentials: 'include'
        })
            .then(response => {
                if (!response.ok) {
                    throw new Error(`HTTP error! status: ${response.status}`);
                }
                return response.json();
            })
            .then(result => {
                if (result.success) {
                    // Update UI - remove unread class and reload
                    loadNotifications();
                }
            })
            .catch(error => {
                console.error('Error marking notification as read:', error);
            });
    }

    /**
     * Mark all notifications as read
     */
    function markAllNotificationsRead() {
        fetch(MARK_ALL_READ_API, {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json',
            },
            credentials: 'include'
        })
            .then(response => {
                if (!response.ok) {
                    throw new Error(`HTTP error! status: ${response.status}`);
                }
                return response.json();
            })
            .then(result => {
                if (result.success) {
                    // Reload notifications to reflect changes
                    loadNotifications();
                }
            })
            .catch(error => {
                console.error('Error marking all notifications as read:', error);
            });
    }

    /**
     * Start auto-refresh of notifications
     */
    function startAutoRefresh() {
        // Refresh every 30 seconds
        autoRefreshTimer = setInterval(() => {
            loadNotifications();
        }, AUTO_REFRESH_INTERVAL);
    }

    /**
     * Stop auto-refresh of notifications
     */
    function stopAutoRefresh() {
        if (autoRefreshTimer) {
            clearInterval(autoRefreshTimer);
            autoRefreshTimer = null;
        }
    }

    /**
     * Escape HTML to prevent XSS
     */
    function escapeHtml(text) {
        const map = {
            '&': '&amp;',
            '<': '&lt;',
            '>': '&gt;',
            '"': '&quot;',
            "'": '&#039;'
        };
        return text.replace(/[&<>"']/g, m => map[m]);
    }

    /**
     * Public API
     */
    window.NotificationLoader = {
        init: init,
        loadNotifications: loadNotifications,
        markNotificationRead: markNotificationRead,
        markAllNotificationsRead: markAllNotificationsRead,
        stopAutoRefresh: stopAutoRefresh
    };

    // Auto-initialize when DOM is ready
    if (document.readyState === 'loading') {
        document.addEventListener('DOMContentLoaded', init);
    } else {
        init();
    }
})();
