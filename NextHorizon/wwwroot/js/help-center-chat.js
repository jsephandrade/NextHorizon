const chatThread = document.getElementById('chatThread');
const agentInput = document.getElementById('agentInput');
const typingIndicator = document.getElementById('typingIndicator');
const chatInputRow = document.getElementById('chatInputRow');
const supportBody = document.querySelector('.support-body');

const categoryData = {
  orders: [
    { text: 'How do I update order status?', icon: 'fas fa-edit' },
    { text: 'How do I cancel an order?', icon: 'fas fa-times' },
    { text: 'How do I view order details?', icon: 'fas fa-eye' },
    { text: 'How do I process returns?', icon: 'fas fa-undo' },
  ],
  refunds: [
    { text: 'How do I process a refund?', icon: 'fas fa-dollar-sign' },
    { text: 'What are refund policies?', icon: 'fas fa-file-contract' },
    { text: 'How long do refunds take?', icon: 'fas fa-clock' },
    { text: 'How do I dispute a refund?', icon: 'fas fa-gavel' },
  ],
  shipping: [
    { text: 'How do I track shipments?', icon: 'fas fa-search' },
    { text: 'How do I update shipping info?', icon: 'fas fa-truck' },
    {
      text: 'What shipping carriers do you use?',
      icon: 'fas fa-shipping-fast',
    },
    {
      text: 'How do I handle lost packages?',
      icon: 'fas fa-exclamation-triangle',
    },
  ],
  payouts: [
    { text: 'When do I get paid?', icon: 'fas fa-calendar' },
    { text: 'How do I view payout history?', icon: 'fas fa-history' },
    { text: 'Why was my payout delayed?', icon: 'fas fa-clock' },
    { text: 'How do I update payout method?', icon: 'fas fa-credit-card' },
  ],
};

function formatDate() {
  const d = new Date();
  return d.toLocaleTimeString([], { hour: '2-digit', minute: '2-digit' });
}

function addMessage(text, type) {
  const msg = document.createElement('div');
  msg.className = 'chat-message ' + type;
  msg.innerHTML = `<div>${text}</div><div class='chat-ts'>${formatDate()}</div>`;
  chatThread.appendChild(msg);
  supportBody.scrollTop = supportBody.scrollHeight;
}

function setTyping(visible) {
  typingIndicator.style.display = visible ? 'block' : 'none';
}

function showMainCategories(showText = false) {
  const existingButtons = chatThread.querySelectorAll('.message-buttons');
  existingButtons.forEach((btn) => btn.remove());

  const categoryMessage = document.createElement('div');
  categoryMessage.className = 'chat-message bot message-buttons';
  categoryMessage.innerHTML = `
    ${showText ? '<div>How can I help you today?</div>' : ''}
    <div class="message-buttons-grid">
    <div class="buttons-two-columns">
    <div class="button-column">
    <button class="message-btn category-btn" data-category="orders">
    <i class="fas fa-box"></i>
    <span>Manage Orders</span>
    </button>
    <button class="message-btn category-btn" data-category="refunds">
    <i class="fas fa-undo"></i>
    <span>Refund Requests</span>
    </button>
    </div>
    <div class="button-column">
    <button class="message-btn category-btn" data-category="shipping">
    <i class="fas fa-truck"></i>
    <span>Shipping Issues</span>
    </button>
    <button class="message-btn category-btn" data-category="payouts">
    <i class="fas fa-wallet"></i>
    <span>Payouts</span>
    </button>
    </div>
    </div>
    <button class="message-btn agent-chat-btn" type="button">
    <i class="fas fa-comments"></i>
    <span>Chat with Agent</span>
    </button>
    </div>
    `;
  chatThread.appendChild(categoryMessage);
  supportBody.scrollTop = supportBody.scrollHeight;

  categoryMessage.querySelectorAll('.category-btn').forEach((button) => {
    button.addEventListener('click', () => {
      showCategoryQuestions(button.getAttribute('data-category'));
    });
  });

  const agentButton = categoryMessage.querySelector('.agent-chat-btn');
  if (agentButton) {
    agentButton.addEventListener('click', () => {
      openChatInput();
    });
  }
}

function showCategoryQuestions(category) {
  const existingButtons = chatThread.querySelectorAll('.message-buttons');
  existingButtons.forEach((btn) => btn.remove());

  const labels = {
    orders: 'Order Management',
    refunds: 'Refund Requests',
    shipping: 'Shipping Issues',
    payouts: 'Payouts',
  };

  const questionMessage = document.createElement('div');
  questionMessage.className = 'chat-message bot message-buttons';
  questionMessage.innerHTML = `
    <div>Select a question about ${labels[category]}:</div>
    <div class="message-buttons-grid">
    ${categoryData[category]
      .map(
        (q, i) => `
    <button class="message-btn question-btn" data-index="${i}">
    <i class="${q.icon}"></i>
    <span>${q.text}</span>
    </button>
    `
      )
      .join('')}
    </div>
    `;
  chatThread.appendChild(questionMessage);
  supportBody.scrollTop = supportBody.scrollHeight;

  questionMessage.querySelectorAll('.question-btn').forEach((button) => {
    button.addEventListener('click', () => {
      const idx = Number(button.getAttribute('data-index'));
      handleQuestion(category, categoryData[category][idx].text);
    });
  });
}

function handleQuestion(category, question) {
  addMessage(question, 'user');
  setTyping(true);

  setTimeout(() => {
    setTyping(false);
    const names = {
      orders: 'order management',
      refunds: 'refund processing',
      shipping: 'shipping',
      payouts: 'payouts',
    };
    addMessage(
      `Agent: Here’s some help for ${names[category]}: (Sample answer for \"${question}\").`,
      'bot'
    );

    setTimeout(showMainCategories, 2000);
  }, 1000);
}

function openChatInput() {
  chatThread
    .querySelectorAll('.message-buttons')
    .forEach((btn) => btn.remove());
  chatInputRow.classList.remove('hidden');
  agentInput.focus();
  addMessage(
    'Agent: Hello! I am here to help. Type your message below.',
    'bot'
  );
}

function clearChat() {
  chatThread.innerHTML = '';
  setTyping(false);
  showMainCategories(false);
}

function sendAgentMessage() {
  const text = agentInput.value.trim();
  if (!text) return;
  addMessage(text, 'user');
  agentInput.value = '';
  setTyping(true);

  setTimeout(() => {
    setTyping(false);
    addMessage(
      'Agent: Thanks for your message! We are looking into this and will respond shortly.',
      'bot'
    );
    // Add back to categories button
    const backMessage = document.createElement('div');
    backMessage.className = 'chat-message bot message-buttons';
    backMessage.innerHTML = `
        <div>Need help with something else?</div>
        <div class="message-buttons-grid">
        <button class="message-btn back-btn" type="button">
        <i class="fas fa-arrow-left"></i>
        <span>Back to Categories</span>
        </button>
        </div>
        `;
    chatThread.appendChild(backMessage);
    supportBody.scrollTop = supportBody.scrollHeight;
    backMessage.querySelector('.back-btn').addEventListener('click', () => {
      showMainCategories(false);
    });
  }, 700);
}

agentInput.addEventListener('keypress', (event) => {
  if (event.key === 'Enter') {
    event.preventDefault();
    sendAgentMessage();
  }
});

if (document.readyState === 'loading') {
  document.addEventListener('DOMContentLoaded', () => showMainCategories(true));
} else {
  showMainCategories(true);
}

window.addEventListener('load', function () {
  const left = document.querySelector('.help-left');
  const right = document.querySelector('.help-right');
  if (left && right) {
    right.style.maxHeight = left.offsetHeight + 'px';
  }
});

window.addEventListener('resize', function () {
  const left = document.querySelector('.help-left');
  const right = document.querySelector('.help-right');
  if (left && right) {
    right.style.maxHeight = left.offsetHeight + 'px';
  }
});
