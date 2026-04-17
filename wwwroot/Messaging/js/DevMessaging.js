(function () {
  function byId(id) { return document.getElementById(id); }
  function setText(id, value) { const node = byId(id); if (node) node.textContent = value; }
  function parseJson(response) { return response.json().catch(function () { return null; }); }
  function toPositiveInt(value) {
    const parsed = Number.parseInt(String(value || '').trim(), 10);
    return Number.isInteger(parsed) && parsed > 0 ? parsed : null;
  }
  function formatJson(value) { return JSON.stringify(value, null, 2); }
  function formatTime(value) {
    const parsed = new Date(value);
    return Number.isNaN(parsed.getTime()) ? '' : parsed.toLocaleString();
  }

  function normalizeMessages(payload) {
    if (!Array.isArray(payload)) return [];
    return payload
      .map(function (item) {
        return {
          senderUserId: String(item.senderUserId || item.SenderUserId || ''),
          body: typeof (item.body ?? item.Body) === 'string' ? (item.body ?? item.Body) : '',
          attachmentUrl: typeof (item.attachmentUrl ?? item.AttachmentUrl) === 'string'
            ? (item.attachmentUrl ?? item.AttachmentUrl)
            : '',
          sentAt: item.sentAt || item.SentAt,
          isDeleted: Boolean(item.isDeleted ?? item.IsDeleted),
        };
      })
      .filter(function (item) { return item.senderUserId; })
      .sort(function (a, b) { return new Date(a.sentAt) - new Date(b.sentAt); });
  }

  async function request(url, options) {
    const response = await fetch(url, Object.assign({ credentials: 'same-origin' }, options || {}));
    const payload = await parseJson(response);
    if (!response.ok) {
      const error = new Error(
        payload?.title ||
        payload?.message ||
        (typeof payload === 'string' ? payload : '') ||
        response.statusText ||
        'Request failed.');
      error.status = response.status;
      error.payload = payload;
      throw error;
    }
    return payload;
  }

  function showResponse(method, url, payload) {
    setText('responseMeta', method + ' ' + url);
    setText('responseBody', formatJson(payload ?? {}));
  }

  async function invoke(method, url, options) {
    try {
      const payload = await request(url, Object.assign({ method: method }, options || {}));
      showResponse(method, url, payload ?? {});
      return payload;
    } catch (error) {
      showResponse(method, url, {
        status: error.status || 500,
        message: error.message,
        payload: error.payload,
      });
      throw error;
    }
  }

  function getActorMode() {
    return document.querySelector('input[name="actorMode"]:checked')?.value || 'buyer';
  }

  function applyPreset() {
    const preset = byId('presetSelect')?.value || '';
    if (!preset) return;
    const [buyerUserId, sellerUserId, orderId] = preset.split('|');
    if (!buyerUserId || !sellerUserId || !orderId) return;

    if (byId('buyerUserId')) byId('buyerUserId').value = buyerUserId;
    if (byId('sellerUserId')) byId('sellerUserId').value = sellerUserId;
    if (byId('orderId')) byId('orderId').value = orderId;
    if (byId('chatBuyerUserId')) byId('chatBuyerUserId').value = buyerUserId;
    if (byId('chatSellerUserId')) byId('chatSellerUserId').value = sellerUserId;
    if (byId('chatOrderId')) byId('chatOrderId').value = orderId;
    syncActorUserId();
  }

  function syncActorUserId() {
    const actorMode = getActorMode();
    const nextValue = actorMode === 'seller'
      ? String(byId('sellerUserId')?.value || '')
      : String(byId('buyerUserId')?.value || '');
    if (byId('actorUserId') && nextValue) byId('actorUserId').value = nextValue;
  }

  function readForm() {
    return {
      actorUserId: String(byId('actorUserId')?.value || '').trim(),
      buyerUserId: String(byId('buyerUserId')?.value || '').trim(),
      sellerUserId: String(byId('sellerUserId')?.value || '').trim(),
      orderId: toPositiveInt(byId('orderId')?.value),
      conversationId: toPositiveInt(byId('conversationId')?.value),
      messageId: toPositiveInt(byId('messageId')?.value),
      body: String(byId('messageBody')?.value || '').trim(),
      attachment: byId('messageAttachment')?.files?.[0] || null,
    };
  }

  const preview = {
    buyerUserId: null,
    sellerUserId: null,
    orderId: null,
    conversationId: null,
    mode: 'general',
  };

  function renderThread(threadId, messages, actorUserId) {
    const thread = byId(threadId);
    if (!thread) return;
    thread.innerHTML = '';

    if (!messages.length) {
      const empty = document.createElement('div');
      empty.className = 'chat-empty';
      empty.textContent = 'No messages yet.';
      thread.appendChild(empty);
      return;
    }

    messages.forEach(function (message) {
      const row = document.createElement('div');
      row.className = 'seller-chat-message ' + (message.senderUserId === String(actorUserId) ? 'buyer' : 'seller');

      const bubble = document.createElement('div');
      bubble.className = 'bubble';
      if (message.isDeleted) bubble.textContent = '[message deleted]';
      else if (message.body) bubble.textContent = message.body;

      if (!message.isDeleted && message.attachmentUrl) {
        const image = document.createElement('img');
        image.className = 'bubble-attachment-image';
        image.src = message.attachmentUrl;
        image.alt = 'Attachment';
        image.loading = 'lazy';
        bubble.appendChild(image);
      }

      const meta = document.createElement('div');
      meta.className = 'bubble-meta';
      meta.textContent = message.senderUserId + ' at ' + formatTime(message.sentAt);
      bubble.appendChild(meta);

      row.appendChild(bubble);
      thread.appendChild(row);
    });

    thread.scrollTop = thread.scrollHeight;
  }

  function updateAttachmentLabel(role) {
    const inputId = role === 'seller' ? 'sellerAttachment' : 'buyerAttachment';
    const labelId = role === 'seller' ? 'sellerAttachmentName' : 'buyerAttachmentName';
    const fileName = byId(inputId)?.files?.[0]?.name || 'No attachment';
    setText(labelId, fileName);
  }

  function toggleOrderField() {
    const field = byId('chatOrderField');
    if (!field) return;
    field.style.display = (byId('chatMode')?.value || 'general') === 'order' ? 'block' : 'none';
  }

  async function createGeneralConversation() {
    const form = readForm();
    const payload = await invoke('POST', '/api/dev/messages/conversations/general', {
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({
        actorUserId: form.actorUserId,
        buyerUserId: form.buyerUserId,
        sellerUserId: form.sellerUserId,
      }),
    });

    const conversationId = payload?.conversationId || payload?.ConversationId;
    if (conversationId && byId('conversationId')) byId('conversationId').value = String(conversationId);
  }

  async function createOrderConversation() {
    const form = readForm();
    const payload = await invoke('POST', '/api/dev/messages/conversations/order', {
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({
        actorUserId: form.actorUserId,
        orderId: form.orderId || 0,
        buyerUserId: form.buyerUserId || null,
        sellerUserId: form.sellerUserId || null,
      }),
    });

    const conversationId = payload?.conversationId || payload?.ConversationId;
    if (conversationId && byId('conversationId')) byId('conversationId').value = String(conversationId);
  }

  async function listConversations() {
    const form = readForm();
    await invoke('GET', '/api/dev/messages/conversations?actorUserId=' + encodeURIComponent(form.actorUserId) + '&page=1&pageSize=50');
  }

  async function getConversation() {
    const form = readForm();
    if (!form.conversationId) throw new Error('Conversation ID is required.');
    await invoke('GET', '/api/dev/messages/conversations/' + form.conversationId + '?actorUserId=' + encodeURIComponent(form.actorUserId));
  }

  async function markRead() {
    const form = readForm();
    if (!form.conversationId) throw new Error('Conversation ID is required.');
    await invoke('POST', '/api/dev/messages/conversations/' + form.conversationId + '/read', {
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({ actorUserId: form.actorUserId }),
    });
  }

  async function sendMessage() {
    const form = readForm();
    if (!form.conversationId) throw new Error('Conversation ID is required.');

    const payload = new FormData();
    payload.append('actorUserId', form.actorUserId);
    if (form.body) payload.append('body', form.body);
    if (form.attachment) payload.append('attachment', form.attachment);

    const response = await invoke('POST', '/api/dev/messages/conversations/' + form.conversationId + '/messages', {
      body: payload,
    });

    const messageId = response?.messageId || response?.MessageId;
    if (messageId && byId('messageId')) byId('messageId').value = String(messageId);
  }

  async function listMessages() {
    const form = readForm();
    if (!form.conversationId) throw new Error('Conversation ID is required.');
    await invoke('GET', '/api/dev/messages/conversations/' + form.conversationId + '/messages?actorUserId=' + encodeURIComponent(form.actorUserId) + '&pageSize=100');
  }

  async function deleteMessage() {
    const form = readForm();
    if (!form.messageId) throw new Error('Message ID is required.');
    await invoke('DELETE', '/api/dev/messages/messages/' + form.messageId + '?actorUserId=' + encodeURIComponent(form.actorUserId));
  }

  async function createPreviewConversation(actorUserId) {
    const buyerUserId = toPositiveInt(preview.buyerUserId);
    const sellerUserId = toPositiveInt(preview.sellerUserId);
    if (!buyerUserId || !sellerUserId) throw new Error('Buyer and seller user IDs are required.');

    if (preview.mode === 'order') {
      const orderId = toPositiveInt(preview.orderId);
      if (!orderId) throw new Error('Order ID is required for order mode.');

      return invoke('POST', '/api/dev/messages/conversations/order', {
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({
          actorUserId: String(actorUserId),
          orderId: orderId,
          buyerUserId: String(buyerUserId),
          sellerUserId: String(sellerUserId),
        }),
      });
    }

    return invoke('POST', '/api/dev/messages/conversations/general', {
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({
        actorUserId: String(actorUserId),
        buyerUserId: String(buyerUserId),
        sellerUserId: String(sellerUserId),
      }),
    });
  }

  async function loadPreviewMessages() {
    if (!preview.conversationId || !preview.buyerUserId || !preview.sellerUserId) return;

    const buyerPayload = await invoke('GET', '/api/dev/messages/conversations/' + preview.conversationId + '/messages?actorUserId=' + encodeURIComponent(preview.buyerUserId) + '&pageSize=100');
    const sellerPayload = await invoke('GET', '/api/dev/messages/conversations/' + preview.conversationId + '/messages?actorUserId=' + encodeURIComponent(preview.sellerUserId) + '&pageSize=100');

    renderThread('buyerThread', normalizeMessages(buyerPayload), preview.buyerUserId);
    renderThread('sellerThread', normalizeMessages(sellerPayload), preview.sellerUserId);
  }

  async function startDbChat() {
    preview.mode = byId('chatMode')?.value || 'general';
    preview.buyerUserId = String(byId('chatBuyerUserId')?.value || '').trim();
    preview.sellerUserId = String(byId('chatSellerUserId')?.value || '').trim();
    preview.orderId = byId('chatOrderId')?.value || '';

    const buyerConversation = await createPreviewConversation(preview.buyerUserId);
    preview.conversationId = buyerConversation?.conversationId || buyerConversation?.ConversationId || null;
    setText('chatConversationId', String(preview.conversationId || 'Not started'));
    await createPreviewConversation(preview.sellerUserId);

    setText('buyerPanelUserId', 'Buyer User ID: ' + preview.buyerUserId);
    setText('sellerPanelUserId', 'Seller User ID: ' + preview.sellerUserId);
    await loadPreviewMessages();
  }

  async function sendPreviewMessage(role) {
    if (!preview.conversationId) await startDbChat();

    const actorUserId = role === 'seller' ? preview.sellerUserId : preview.buyerUserId;
    const inputId = role === 'seller' ? 'sellerInput' : 'buyerInput';
    const attachmentId = role === 'seller' ? 'sellerAttachment' : 'buyerAttachment';
    const input = byId(inputId);
    const attachmentInput = byId(attachmentId);
    const body = String(input?.value || '').trim();
    const attachment = attachmentInput?.files?.[0] || null;
    if (!body && !attachment) return;

    const payload = new FormData();
    payload.append('actorUserId', String(actorUserId));
    if (body) payload.append('body', body);
    if (attachment) payload.append('attachment', attachment);

    await invoke('POST', '/api/dev/messages/conversations/' + preview.conversationId + '/messages', { body: payload });
    if (input) input.value = '';
    if (attachmentInput) attachmentInput.value = '';
    updateAttachmentLabel(role);
    await loadPreviewMessages();
  }

  function guard(action) {
    return function () {
      action().catch(function () {});
    };
  }

  byId('presetSelect')?.addEventListener('change', applyPreset);
  document.querySelectorAll('input[name="actorMode"]').forEach(function (radio) {
    radio.addEventListener('change', syncActorUserId);
  });
  byId('buyerUserId')?.addEventListener('input', syncActorUserId);
  byId('sellerUserId')?.addEventListener('input', syncActorUserId);
  byId('chatMode')?.addEventListener('change', toggleOrderField);

  byId('buyerAttachment')?.addEventListener('change', function () { updateAttachmentLabel('buyer'); });
  byId('sellerAttachment')?.addEventListener('change', function () { updateAttachmentLabel('seller'); });
  byId('buyerClearAttachmentBtn')?.addEventListener('click', function () {
    if (byId('buyerAttachment')) byId('buyerAttachment').value = '';
    updateAttachmentLabel('buyer');
  });
  byId('sellerClearAttachmentBtn')?.addEventListener('click', function () {
    if (byId('sellerAttachment')) byId('sellerAttachment').value = '';
    updateAttachmentLabel('seller');
  });

  byId('createGeneralBtn')?.addEventListener('click', guard(createGeneralConversation));
  byId('createOrderBtn')?.addEventListener('click', guard(createOrderConversation));
  byId('listConversationsBtn')?.addEventListener('click', guard(listConversations));
  byId('getConversationBtn')?.addEventListener('click', guard(getConversation));
  byId('markReadBtn')?.addEventListener('click', guard(markRead));
  byId('sendMessageBtn')?.addEventListener('click', guard(sendMessage));
  byId('listMessagesBtn')?.addEventListener('click', guard(listMessages));
  byId('deleteMessageBtn')?.addEventListener('click', guard(deleteMessage));
  byId('startDbChatBtn')?.addEventListener('click', guard(startDbChat));
  byId('reloadChatBtn')?.addEventListener('click', guard(loadPreviewMessages));
  byId('buyerSendBtn')?.addEventListener('click', guard(function () { return sendPreviewMessage('buyer'); }));
  byId('sellerSendBtn')?.addEventListener('click', guard(function () { return sendPreviewMessage('seller'); }));
  byId('buyerInput')?.addEventListener('keydown', function (event) {
    if (event.key === 'Enter') {
      event.preventDefault();
      sendPreviewMessage('buyer').catch(function () {});
    }
  });
  byId('sellerInput')?.addEventListener('keydown', function (event) {
    if (event.key === 'Enter') {
      event.preventDefault();
      sendPreviewMessage('seller').catch(function () {});
    }
  });

  toggleOrderField();
  applyPreset();
  syncActorUserId();
  updateAttachmentLabel('buyer');
  updateAttachmentLabel('seller');
})();
