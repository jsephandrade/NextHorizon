// messenger.js (hot-reload safe)

function initMessenger() {
  const messageInput = document.querySelector('.message-input');
  const messagesContainer = document.querySelector('.messages-container');
  const searchInput = document.querySelector('.search-input');
  const conversationContainer = document.querySelector('.conversations-list');

  if (
    !messageInput ||
    !messagesContainer ||
    !searchInput ||
    !conversationContainer
  )
    return;

  // =========================
  // TIME FUNCTION
  // =========================
  function getCurrentTime() {
    const now = new Date();
    return now.toLocaleTimeString([], { hour: '2-digit', minute: '2-digit' });
  }

  function updateExistingMessageTimes() {
    const allTimes = document.querySelectorAll('.message-time');
    allTimes.forEach((timeElement) => {
      timeElement.textContent = getCurrentTime();
    });
  }

  updateExistingMessageTimes();

  // =========================
  // SEND MESSAGE FUNCTION
  // =========================
  function sendMessage() {
    const messageText = messageInput.value.trim();
    if (messageText === '') return;

    const messageDiv = document.createElement('div');
    messageDiv.className = 'message sent';

    const contentDiv = document.createElement('div');
    contentDiv.className = 'message-content';

    const paragraphDiv = document.createElement('p');
    paragraphDiv.textContent = messageText;
    contentDiv.appendChild(paragraphDiv);

    const timeSpan = document.createElement('span');
    timeSpan.className = 'message-time';
    timeSpan.textContent = getCurrentTime();

    messageDiv.appendChild(contentDiv);
    messageDiv.appendChild(timeSpan);
    messagesContainer.appendChild(messageDiv);

    messageInput.value = '';
    messagesContainer.scrollTop = messagesContainer.scrollHeight;

    // =========================
    // AUTO REPLY (DEMO)
    // =========================
    setTimeout(() => {
      const replyDiv = document.createElement('div');
      replyDiv.className = 'message received';

      const replyContentDiv = document.createElement('div');
      replyContentDiv.className = 'message-content';

      const replyParagraphDiv = document.createElement('p');
      replyParagraphDiv.textContent =
        "Thanks for your message! I'll get back to you soon.";
      replyContentDiv.appendChild(replyParagraphDiv);

      const replyTimeSpan = document.createElement('span');
      replyTimeSpan.className = 'message-time';
      replyTimeSpan.textContent = getCurrentTime();

      replyDiv.appendChild(replyContentDiv);
      replyDiv.appendChild(replyTimeSpan);

      messagesContainer.appendChild(replyDiv);
      messagesContainer.scrollTop = messagesContainer.scrollHeight;
    }, 1500);
  }

  // =========================
  // CONVERSATION CLICK (Delegated)
  // =========================
  function handleConversationClick(item) {
    const conversationItems =
      conversationContainer.querySelectorAll('.conversation-item');
    conversationItems.forEach((i) => i.classList.remove('active'));
    item.classList.add('active');

    const nameElement = item.querySelector('.conversation-name');
    const chatUserName = document.querySelector('.chat-user-info h3');
    if (nameElement && chatUserName) {
      chatUserName.textContent = nameElement.textContent;
    }

    updateExistingMessageTimes();
  }

  conversationContainer.addEventListener('click', function (e) {
    const item = e.target.closest('.conversation-item');
    if (item) handleConversationClick(item);
  });

  // =========================
  // SEND BUTTON & ENTER KEY (Delegated)
  // =========================
  document.addEventListener('click', function (e) {
    if (e.target.closest('.send-btn')) sendMessage();
  });

  document.addEventListener('keypress', function (e) {
    if (
      e.target.closest('.message-input') &&
      e.key === 'Enter' &&
      !e.shiftKey
    ) {
      e.preventDefault();
      sendMessage();
    }
  });

  // =========================
  // SEARCH CONVERSATIONS
  // =========================
  let noResults = conversationContainer.querySelector('.no-results');
  if (!noResults) {
    noResults = document.createElement('div');
    noResults.textContent = 'No conversations found';
    noResults.className = 'no-results';
    conversationContainer.appendChild(noResults);
  }

  function debounce(func, delay) {
    let timeout;
    return function () {
      clearTimeout(timeout);
      timeout = setTimeout(func, delay);
    };
  }

  function escapeRegExp(string) {
    return string.replace(/[.*+?^${}()|[\]\\]/g, '\\$&');
  }

  function filterConversations() {
    const searchValue = searchInput.value.trim().toLowerCase();
    const items = conversationContainer.querySelectorAll('.conversation-item');
    let visibleCount = 0;
    let firstMatch = null;

    items.forEach((item) => {
      const nameElement = item.querySelector('.conversation-name');
      const lastMessageElement = item.querySelector('.last-message');

      const originalName =
        nameElement.dataset.originalText || nameElement.textContent;
      const originalLastMessage =
        lastMessageElement.dataset.originalText ||
        lastMessageElement.textContent;

      if (!nameElement.dataset.originalText)
        nameElement.dataset.originalText = originalName;
      if (!lastMessageElement.dataset.originalText)
        lastMessageElement.dataset.originalText = originalLastMessage;

      const nameLower = originalName.toLowerCase();
      const lastMessageLower = originalLastMessage.toLowerCase();

      if (
        nameLower.includes(searchValue) ||
        lastMessageLower.includes(searchValue)
      ) {
        item.style.display = 'flex';
        visibleCount++;

        if (searchValue !== '') {
          const regex = new RegExp(`(${escapeRegExp(searchValue)})`, 'gi');
          nameElement.innerHTML = originalName.replace(
            regex,
            '<mark>$1</mark>'
          );
          lastMessageElement.innerHTML = originalLastMessage.replace(
            regex,
            '<mark>$1</mark>'
          );
        } else {
          nameElement.innerHTML = originalName;
          lastMessageElement.innerHTML = originalLastMessage;
        }

        if (!firstMatch) firstMatch = item;
      } else {
        item.style.display = 'none';
        nameElement.innerHTML = originalName;
        lastMessageElement.innerHTML = originalLastMessage;
      }
    });

    noResults.style.display = visibleCount === 0 ? 'block' : 'none';

    if (firstMatch) handleConversationClick(firstMatch);
  }

  searchInput.addEventListener('input', debounce(filterConversations, 200));
}

// =========================
// INITIALIZE ON LOAD
// =========================
document.addEventListener('DOMContentLoaded', initMessenger);

// =========================
// HOT RELOAD SUPPORT
// =========================
if (module.hot) {
  module.hot.accept(initMessenger);
}
