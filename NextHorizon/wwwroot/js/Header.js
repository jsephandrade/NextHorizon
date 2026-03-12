function getNotifElements() {
  return {
    wrap: document.getElementById('notif-wrap'),
    button: document.getElementById('notif-btn'),
    dropdown: document.getElementById('notif-dropdown'),
    dot: document.getElementById('notif-dot'),
    list: document.getElementById('notif-list')
  };
}

function getProfileElements() {
  return {
    wrap: document.getElementById('profile-wrap'),
    button: document.getElementById('profile-btn'),
    dropdown: document.getElementById('profile-dropdown')
  };
}

function closeNotifDropdown() {
  const { dropdown } = getNotifElements();
  dropdown?.classList.remove('open');
}

function closeProfileDropdown() {
  const { button, dropdown } = getProfileElements();
  dropdown?.classList.remove('open');
  button?.setAttribute('aria-expanded', 'false');
}

function markNotificationsRead() {
  const { dot } = getNotifElements();
  document.querySelectorAll('.notif-item.unread').forEach(item => item.classList.remove('unread'));
  dot?.classList.remove('active');
}

function toggleNotifDropdown() {
  const { dropdown } = getNotifElements();
  if (!dropdown) {
    return;
  }

  closeProfileDropdown();
  const isOpen = dropdown.classList.toggle('open');
  if (isOpen) {
    markNotificationsRead();
  }
}

function clearNotifications() {
  const { list, dot } = getNotifElements();
  if (list) {
    list.innerHTML = '<li style="padding:20px 16px;text-align:center;color:#9ca3af;font-size:13px;">No notifications</li>';
  }

  dot?.classList.remove('active');
}

function toggleProfileDropdown() {
  const { button, dropdown } = getProfileElements();
  if (!button || !dropdown) {
    return;
  }

  closeNotifDropdown();
  const isOpen = dropdown.classList.toggle('open');
  button.setAttribute('aria-expanded', isOpen ? 'true' : 'false');
}

document.addEventListener('DOMContentLoaded', function () {
  const { dot } = getNotifElements();
  if (dot && document.querySelectorAll('.notif-item.unread').length > 0) {
    dot.classList.add('active');
  }
});

document.addEventListener('click', function (event) {
  const { wrap: notifWrap } = getNotifElements();
  const { wrap: profileWrap } = getProfileElements();

  if (notifWrap && !notifWrap.contains(event.target)) {
    closeNotifDropdown();
  }

  if (profileWrap && !profileWrap.contains(event.target)) {
    closeProfileDropdown();
  }
});
