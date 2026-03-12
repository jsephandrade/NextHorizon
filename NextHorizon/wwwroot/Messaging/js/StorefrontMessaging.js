(function () {
  const DEFAULT_ERROR = 'Could not load messages right now.';
  const AUTH_ERROR = 'Messaging is not available for this session.';
  const NOT_FOUND_ERROR = 'Conversation is not available right now.';
  const IMAGE_EXTENSIONS = ['.jpg', '.jpeg', '.png', '.webp'];
  const VIDEO_EXTENSIONS = ['.mp4', '.webm', '.mov'];
  let csrfTokenPromise = null;

  function byId(id) { return id ? document.getElementById(id) : null; }

  function getFileExtension(value) {
    if (typeof value !== 'string' || !value.trim()) return '';
    const sanitized = value.split('#')[0].split('?')[0].trim().toLowerCase();
    const dotIndex = sanitized.lastIndexOf('.');
    return dotIndex >= 0 ? sanitized.slice(dotIndex) : '';
  }

  function getAttachmentKind(value) {
    const extension = getFileExtension(value);
    if (IMAGE_EXTENSIONS.indexOf(extension) >= 0) return 'image';
    if (VIDEO_EXTENSIONS.indexOf(extension) >= 0) return 'video';
    return '';
  }

  function formatMessageTime(value) {
    const date = new Date(value);
    if (Number.isNaN(date.getTime())) return '';
    return date.toLocaleTimeString([], { hour: 'numeric', minute: '2-digit' });
  }

  function parseJson(response) {
    return response.text().then(function (text) {
      if (!text) return null;
      try {
        return JSON.parse(text);
      } catch (_) {
        return text;
      }
    }).catch(function () { return null; });
  }

  function toast(message, type) {
    if (window.toast && typeof window.toast[type || 'info'] === 'function') {
      window.toast[type || 'info'](message);
      return;
    }
    console.warn(message);
  }

  function friendlyError(status, payload) {
    if (status === 401 || status === 403) return AUTH_ERROR;
    if (status === 404) return NOT_FOUND_ERROR;
    if (typeof payload === 'string' && payload.trim()) return payload;
    if (typeof payload?.title === 'string' && payload.title.trim()) return payload.title;
    if (typeof payload?.message === 'string' && payload.message.trim()) return payload.message;
    return DEFAULT_ERROR;
  }

  function normalizeMessages(payload) {
    if (!Array.isArray(payload)) return [];
    return payload
      .map(function (item) {
        return {
          senderUserId: String(item.senderUserId || item.SenderUserId || ''),
          body: typeof (item.body ?? item.Body) === 'string' ? (item.body ?? item.Body) : '',
          sentAt: String(item.sentAt || item.SentAt || ''),
          isDeleted: Boolean(item.isDeleted ?? item.IsDeleted),
          attachmentUrl: typeof (item.attachmentUrl ?? item.AttachmentUrl) === 'string'
            ? (item.attachmentUrl ?? item.AttachmentUrl)
            : '',
        };
      })
      .filter(function (item) { return item.senderUserId; })
      .sort(function (a, b) { return new Date(a.sentAt) - new Date(b.sentAt); });
  }

  async function getCsrfToken() {
    if (!csrfTokenPromise) {
      csrfTokenPromise = fetch('/api/security/csrf-token', {
        method: 'GET',
        credentials: 'same-origin',
      })
        .then(async function (response) {
          if (!response.ok) throw new Error('Unable to initialize secure messaging.');
          const payload = await response.json();
          return typeof payload?.token === 'string' ? payload.token : '';
        })
        .catch(function (error) {
          csrfTokenPromise = null;
          throw error;
        });
    }

    return csrfTokenPromise;
  }

  async function request(url, options) {
    const response = await fetch(url, Object.assign({ credentials: 'same-origin' }, options || {}));
    const payload = await parseJson(response);
    if (!response.ok) {
      const error = new Error(friendlyError(response.status, payload));
      error.status = response.status;
      error.payload = payload;
      throw error;
    }
    return payload;
  }

  async function requestWithCsrf(url, options) {
    const csrfToken = await getCsrfToken();
    const headers = Object.assign({}, options?.headers || {});
    if (csrfToken) headers['X-CSRF-TOKEN'] = csrfToken;
    return request(url, Object.assign({}, options || {}, { headers: headers }));
  }

  function buildAttachmentNode(url, kind, classNamePrefix) {
    if (kind === 'image') {
      const image = document.createElement('img');
      image.className = classNamePrefix + '-image';
      image.src = url;
      image.alt = 'Attachment preview';
      image.loading = 'lazy';
      return image;
    }

    if (kind === 'video') {
      const video = document.createElement('video');
      video.className = classNamePrefix + '-video';
      video.src = url;
      video.controls = true;
      video.preload = 'metadata';
      video.playsInline = true;
      return video;
    }

    return null;
  }

  function renderBubble(message, buyerUserId) {
    const senderClass = message.senderUserId === buyerUserId ? 'buyer' : 'seller';
    const senderLabel = senderClass === 'buyer' ? 'You' : 'Seller';
    const wrapper = document.createElement('div');
    wrapper.className = 'seller-chat-message ' + senderClass;

    const bubble = document.createElement('div');
    bubble.className = 'bubble';

    if (message.isDeleted) {
      bubble.textContent = '[message deleted]';
    } else if (message.body) {
      const body = document.createElement('div');
      body.className = 'bubble-body';
      body.textContent = message.body;
      bubble.appendChild(body);
    }

    if (!message.isDeleted && message.attachmentUrl) {
      const mediaKind = getAttachmentKind(message.attachmentUrl);
      const media = buildAttachmentNode(message.attachmentUrl, mediaKind, 'bubble-attachment');
      if (media) {
        if (mediaKind === 'image') {
          media.alt = 'Message attachment';
        }
        bubble.appendChild(media);
      }

      const link = document.createElement('a');
      link.className = 'bubble-attachment-link';
      link.href = message.attachmentUrl;
      link.target = '_blank';
      link.rel = 'noopener noreferrer';
      link.textContent = 'Open attachment';
      bubble.appendChild(link);
    }

    const meta = document.createElement('div');
    meta.className = 'bubble-meta';
    meta.textContent = senderLabel + ' at ' + formatMessageTime(message.sentAt);
    bubble.appendChild(meta);

    wrapper.appendChild(bubble);
    return wrapper;
  }

  function createStorefrontMessaging(config) {
    const state = {
      sellerUserId: null,
      sellerName: config.defaultSellerName || 'Seller',
      sellerAvatarUrl: config.defaultSellerAvatarUrl || '',
      buyerUserId: null,
      conversationId: null,
      messages: [],
      headElement: null,
      isLoading: false,
      lastError: '',
      previewObjectUrl: '',
    };

    function modal() { return byId(config.modalId); }
    function thread() { return byId(config.threadId); }
    function input() { return byId(config.inputId); }
    function sendButton() { return byId(config.sendButtonId); }
    function attachmentInput() { return byId(config.attachmentInputId); }
    function attachmentPreview() { return byId(config.attachmentPreviewId); }
    function attachmentStatus() { return byId(config.attachmentStatusId); }
    function clearAttachmentButton() { return byId(config.clearAttachmentButtonId); }
    function titleEl() { return byId(config.titleId); }
    function avatarEl() { return byId(config.avatarId); }
    function headContainer() { return byId(config.headContainerId); }

    function getAttachmentFile() {
      return attachmentInput()?.files?.[0] || null;
    }

    function revokePreviewUrl() {
      if (!state.previewObjectUrl) return;
      URL.revokeObjectURL(state.previewObjectUrl);
      state.previewObjectUrl = '';
    }

    function resizeComposer() {
      const inputNode = input();
      if (!inputNode || inputNode.tagName !== 'TEXTAREA') return;
      inputNode.style.height = 'auto';
      const maxHeight = Number.parseFloat(window.getComputedStyle(inputNode).maxHeight);
      if (Number.isFinite(maxHeight) && maxHeight > 0) {
        inputNode.style.height = Math.min(inputNode.scrollHeight, maxHeight) + 'px';
        inputNode.style.overflowY = inputNode.scrollHeight > maxHeight ? 'auto' : 'hidden';
        return;
      }

      inputNode.style.height = inputNode.scrollHeight + 'px';
      inputNode.style.overflowY = 'hidden';
    }

    function renderAttachmentPreview() {
      const preview = attachmentPreview();
      const file = getAttachmentFile();

      revokePreviewUrl();

      if (!preview) return;

      preview.innerHTML = '';
      preview.hidden = true;

      if (!file) return;

      const mediaKind = getAttachmentKind(file.name);
      if (!mediaKind) return;

      state.previewObjectUrl = URL.createObjectURL(file);
      const media = buildAttachmentNode(state.previewObjectUrl, mediaKind, 'chat-attachment-preview');
      if (!media) return;

      if (mediaKind === 'image') {
        media.alt = 'Selected attachment preview';
      } else if (mediaKind === 'video') {
        media.muted = true;
      }

      preview.appendChild(media);
      preview.hidden = false;
    }

    function updateAttachmentUi() {
      const file = getAttachmentFile();
      const status = attachmentStatus();
      const clearButton = clearAttachmentButton();
      const attachmentNode = attachmentInput();

      if (status) {
        if (file) {
          status.textContent = file.name;
          status.classList.add('is-active');
          status.hidden = false;
        } else {
          status.textContent = '';
          status.classList.remove('is-active');
          status.hidden = true;
        }
      }

      if (clearButton) {
        clearButton.hidden = !file;
        clearButton.disabled = !file || Boolean(attachmentNode?.disabled);
      }

      renderAttachmentPreview();
    }

    function clearAttachment() {
      const inputNode = attachmentInput();
      if (inputNode) inputNode.value = '';
      updateAttachmentUi();
    }

    function setComposer(enabled, placeholder) {
      const inputNode = input();
      const button = sendButton();
      const attachmentNode = attachmentInput();
      const clearButton = clearAttachmentButton();
      if (inputNode) {
        inputNode.disabled = !enabled;
        inputNode.placeholder = placeholder || inputNode.placeholder;
      }
      if (button) button.disabled = !enabled;
      if (attachmentNode) attachmentNode.disabled = !enabled;
      if (clearButton) clearButton.disabled = !enabled || !getAttachmentFile();
    }

    function updateHeader() {
      const title = titleEl();
      if (title) title.textContent = state.sellerName || config.defaultSellerName || 'Seller Chat';
      const avatar = avatarEl();
      if (avatar) {
        avatar.src = state.sellerAvatarUrl || config.defaultSellerAvatarUrl || '';
        avatar.alt = state.sellerName || config.defaultSellerName || 'Seller';
      }
    }

    function renderEmpty(message) {
      const threadNode = thread();
      if (!threadNode) return;
      threadNode.innerHTML = '';
      const empty = document.createElement('div');
      empty.className = 'chat-empty';
      empty.textContent = message;
      threadNode.appendChild(empty);
    }

    function renderQuickMessages() {
      const threadNode = thread();
      if (!threadNode || !config.quickMessages?.length) return;
      const wrap = document.createElement('div');
      wrap.className = 'quick-message-container';
      config.quickMessages.forEach(function (message) {
        const button = document.createElement('button');
        button.type = 'button';
        button.className = 'quick-message-btn';
        button.textContent = message;
        button.addEventListener('click', function () {
          const inputNode = input();
          if (!inputNode || inputNode.disabled) return;
          inputNode.value = message;
          send();
        });
        wrap.appendChild(button);
      });
      threadNode.appendChild(wrap);
    }

    function renderMessages() {
      const threadNode = thread();
      if (!threadNode) return;
      threadNode.innerHTML = '';

      if (state.lastError) {
        renderEmpty(state.lastError);
        return;
      }
      if (state.isLoading) {
        renderEmpty('Loading messages...');
        return;
      }
      if (!state.messages.length) {
        renderEmpty('No messages yet. Start the conversation.');
        renderQuickMessages();
        return;
      }

      state.messages.forEach(function (message) {
        threadNode.appendChild(renderBubble(message, state.buyerUserId || ''));
      });
      threadNode.scrollTop = threadNode.scrollHeight;
    }

    async function ensureConversation() {
      if (!state.sellerUserId) throw new Error('Chat is not ready for this seller yet.');
      if (state.conversationId) return state.conversationId;

      const payload = await requestWithCsrf('/api/messages/conversations', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({
          contextType: config.contextType || 'general',
          sellerUserId: state.sellerUserId,
          orderId: config.orderId || null,
        }),
      });

      const conversationId = Number.parseInt(String(payload.conversationId || payload.ConversationId), 10);
      const buyerUserId = String(payload.buyerUserId || payload.BuyerUserId || '');
      if (!Number.isInteger(conversationId) || conversationId <= 0 || !buyerUserId) {
        throw new Error(DEFAULT_ERROR);
      }

      state.conversationId = conversationId;
      state.buyerUserId = buyerUserId;
      return conversationId;
    }

    async function markRead() {
      if (!state.conversationId) return;
      try {
        await requestWithCsrf('/api/messages/conversations/' + state.conversationId + '/read', {
          method: 'POST',
          headers: { 'Content-Type': 'application/json' },
          body: '{}',
        });
      } catch (error) {
        if (error.status !== 404) console.warn(error);
      }
    }

    async function loadMessages() {
      state.isLoading = true;
      state.lastError = '';
      renderMessages();

      try {
        const conversationId = await ensureConversation();
        const payload = await request('/api/messages/conversations/' + conversationId + '/messages?pageSize=100');
        state.messages = normalizeMessages(payload);
        setComposer(true, 'Type a message...');
        await markRead();
      } catch (error) {
        state.messages = [];
        state.lastError = error.message || DEFAULT_ERROR;
        setComposer(false, 'Messaging unavailable');
        toast(state.lastError, 'info');
      } finally {
        state.isLoading = false;
      }

      renderMessages();
    }

    async function send() {
      const inputNode = input();
      if (!inputNode || inputNode.disabled) return;

      const body = inputNode.value.trim();
      const attachment = getAttachmentFile();
      if (!body && !attachment) return;

      const button = sendButton();
      if (button) button.disabled = true;

      try {
        const conversationId = await ensureConversation();
        const form = new FormData();
        if (body) form.append('body', body);
        if (attachment) form.append('attachment', attachment);
        await requestWithCsrf('/api/messages/conversations/' + conversationId + '/messages', {
          method: 'POST',
          body: form,
        });
        inputNode.value = '';
        resizeComposer();
        clearAttachment();
        await loadMessages();
      } catch (error) {
        state.lastError = error.message || DEFAULT_ERROR;
        renderMessages();
        toast(state.lastError, 'info');
      } finally {
        if (button) button.disabled = false;
      }
    }

    function removeHead() {
      if (state.headElement) {
        state.headElement.remove();
        state.headElement = null;
      }
    }

    function addHead() {
      const container = headContainer();
      if (!container || state.headElement) return;
      const head = document.createElement('div');
      head.className = config.headClassName || 'seller-chat-head';
      head.textContent = config.headTextFactory
        ? config.headTextFactory(state.sellerName)
        : (state.sellerName || 'S').charAt(0).toUpperCase();
      head.addEventListener('click', function () {
        removeHead();
        open();
      });
      container.appendChild(head);
      state.headElement = head;
    }

    function setSeller(meta) {
      const sellerUserId = Number.parseInt(String(meta?.sellerUserId || ''), 10);
      if (!Number.isInteger(sellerUserId) || sellerUserId <= 0) return;

      const changedSeller = state.sellerUserId !== String(sellerUserId);
      state.sellerUserId = String(sellerUserId);
      state.sellerName = meta.sellerName || state.sellerName;
      state.sellerAvatarUrl = meta.sellerAvatarUrl || state.sellerAvatarUrl;
      updateHeader();

      if (changedSeller) {
        state.conversationId = null;
        state.buyerUserId = null;
        state.messages = [];
        state.lastError = '';
        clearAttachment();
      }
    }

    function open() {
      removeHead();
      updateHeader();
      const node = modal();
      if (node) {
        node.classList.add('active');
        node.style.display = 'block';
      }

      if (!state.sellerUserId) {
        state.lastError = 'Chat is not ready for this seller yet.';
        setComposer(false, 'Chat unavailable');
        renderMessages();
        return;
      }

      setComposer(true, 'Type a message...');
      loadMessages();
      input()?.focus();
    }

    function close() {
      const node = modal();
      if (!node) return;
      node.classList.remove('active');
      node.style.display = 'none';
    }

    function minimize() {
      close();
      addHead();
    }

    updateHeader();
    setComposer(false, 'Chat unavailable');
    attachmentInput()?.addEventListener('change', updateAttachmentUi);
    input()?.addEventListener('input', resizeComposer);
    input()?.addEventListener('keydown', function (event) {
      if (event.key !== 'Enter' || event.shiftKey) return;
      event.preventDefault();
      send();
    });
    clearAttachmentButton()?.addEventListener('click', function () {
      clearAttachment();
      input()?.focus();
    });
    resizeComposer();
    updateAttachmentUi();

    return {
      setSeller: setSeller,
      open: open,
      close: close,
      minimize: minimize,
      send: send,
      refresh: loadMessages,
    };
  }

  window.createStorefrontMessaging = createStorefrontMessaging;
})();
