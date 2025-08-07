// CodeAgent Web Application
class CodeAgentApp {
    constructor() {
        this.currentView = 'chat';
        this.currentProvider = 'openai';
        this.chatHistory = [];
        this.currentPath = '';
        this.init();
    }

    init() {
        this.initializeDrawer();
        this.initializeViews();
        this.initializeChatHandlers();
        this.initializeFileHandlers();
        this.initializeConfigHandlers();
        this.loadConfiguration();
        this.showNotification('CodeAgent Web Portal initialized');
    }

    // Drawer and Navigation
    initializeDrawer() {
        const menuButton = document.getElementById('menu-button');
        const drawer = document.getElementById('drawer');
        const scrim = document.querySelector('.mdc-drawer-scrim');
        
        menuButton.addEventListener('click', () => {
            drawer.classList.add('mdc-drawer--open');
        });
        
        scrim.addEventListener('click', () => {
            drawer.classList.remove('mdc-drawer--open');
        });
        
        // Handle navigation
        const navItems = drawer.querySelectorAll('.mdc-list-item');
        navItems.forEach(item => {
            item.addEventListener('click', (e) => {
                e.preventDefault();
                const view = item.dataset.view;
                this.switchView(view);
                drawer.classList.remove('mdc-drawer--open');
                
                // Update active state
                navItems.forEach(nav => nav.classList.remove('mdc-list-item--activated'));
                item.classList.add('mdc-list-item--activated');
            });
        });

        // Settings button
        document.getElementById('settings-button').addEventListener('click', () => {
            this.switchView('config');
        });
    }

    switchView(viewName) {
        const views = document.querySelectorAll('.view');
        views.forEach(view => view.classList.remove('active'));
        
        const targetView = document.getElementById(`${viewName}-view`);
        if (targetView) {
            targetView.classList.add('active');
            this.currentView = viewName;
            
            // Load view-specific data
            if (viewName === 'files') {
                this.loadFiles();
            } else if (viewName === 'config') {
                this.loadConfiguration();
            }
        }
    }

    initializeViews() {
        // Default to chat view
        this.switchView('chat');
    }

    // Chat Functionality
    initializeChatHandlers() {
        const sendButton = document.getElementById('send-button');
        const clearButton = document.getElementById('clear-button');
        const messageInput = document.getElementById('message-input');
        
        sendButton.addEventListener('click', () => this.sendMessage());
        clearButton.addEventListener('click', () => this.clearChat());
        
        messageInput.addEventListener('keydown', (e) => {
            if (e.key === 'Enter' && !e.shiftKey) {
                e.preventDefault();
                this.sendMessage();
            }
        });
    }

    async sendMessage() {
        const messageInput = document.getElementById('message-input');
        const message = messageInput.value.trim();
        
        if (!message) return;
        
        // Clear input
        messageInput.value = '';
        
        // Add user message to chat
        this.addMessage('user', message);
        
        // Show typing indicator
        this.showTypingIndicator();
        
        try {
            const response = await fetch('/api/chat/message', {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json',
                },
                body: JSON.stringify({ message })
            });
            
            if (!response.ok) {
                throw new Error('Failed to send message');
            }
            
            // Handle streaming response
            const reader = response.body.getReader();
            const decoder = new TextDecoder();
            let assistantMessage = '';
            
            this.hideTypingIndicator();
            const messageElement = this.addMessage('assistant', '', true);
            
            while (true) {
                const { done, value } = await reader.read();
                if (done) break;
                
                const chunk = decoder.decode(value);
                const lines = chunk.split('\n');
                
                for (const line of lines) {
                    if (line.startsWith('data: ')) {
                        try {
                            const data = JSON.parse(line.slice(6));
                            if (data.content) {
                                assistantMessage += data.content;
                                this.updateMessage(messageElement, assistantMessage);
                            }
                        } catch (e) {
                            // Ignore JSON parse errors for incomplete chunks
                        }
                    }
                }
            }
        } catch (error) {
            this.hideTypingIndicator();
            this.showNotification('Error: ' + error.message, 'error');
        }
    }

    addMessage(sender, content, streaming = false) {
        const container = document.getElementById('messages-container');
        
        // Remove welcome message if present
        const welcome = container.querySelector('.welcome-message');
        if (welcome) welcome.remove();
        
        const messageDiv = document.createElement('div');
        messageDiv.className = `message ${sender}`;
        
        const headerDiv = document.createElement('div');
        headerDiv.className = 'message-header';
        headerDiv.textContent = sender === 'user' ? 'You' : 'CodeAgent';
        
        const contentDiv = document.createElement('div');
        contentDiv.className = 'message-content';
        contentDiv.textContent = content;
        
        messageDiv.appendChild(headerDiv);
        messageDiv.appendChild(contentDiv);
        container.appendChild(messageDiv);
        
        // Scroll to bottom
        container.scrollTop = container.scrollHeight;
        
        return contentDiv;
    }

    updateMessage(element, content) {
        element.textContent = content;
        const container = document.getElementById('messages-container');
        container.scrollTop = container.scrollHeight;
    }

    showTypingIndicator() {
        const container = document.getElementById('messages-container');
        const indicator = document.createElement('div');
        indicator.className = 'typing-indicator';
        indicator.id = 'typing-indicator';
        indicator.innerHTML = '<span></span><span></span><span></span>';
        container.appendChild(indicator);
        container.scrollTop = container.scrollHeight;
    }

    hideTypingIndicator() {
        const indicator = document.getElementById('typing-indicator');
        if (indicator) indicator.remove();
    }

    clearChat() {
        const container = document.getElementById('messages-container');
        container.innerHTML = `
            <div class="welcome-message">
                <h2>Welcome to CodeAgent</h2>
                <p>Start a conversation by typing a message below.</p>
            </div>
        `;
        
        fetch('/api/chat/reset', { method: 'POST' })
            .then(() => this.showNotification('Chat cleared'))
            .catch(error => this.showNotification('Error clearing chat', 'error'));
    }

    // File Browser Functionality
    initializeFileHandlers() {
        const refreshButton = document.getElementById('refresh-files');
        const closeEditor = document.getElementById('close-editor');
        const saveFile = document.getElementById('save-file');
        const cancelEdit = document.getElementById('cancel-edit');
        
        refreshButton.addEventListener('click', () => this.loadFiles());
        closeEditor.addEventListener('click', () => this.closeFileEditor());
        cancelEdit.addEventListener('click', () => this.closeFileEditor());
        saveFile.addEventListener('click', () => this.saveFile());
    }

    async loadFiles(path = '') {
        try {
            const response = await fetch(`/api/file/browse?path=${encodeURIComponent(path)}`);
            if (!response.ok) throw new Error('Failed to load files');
            
            const data = await response.json();
            this.currentPath = data.currentPath;
            document.getElementById('current-path').value = this.currentPath;
            
            const fileList = document.getElementById('file-list');
            fileList.innerHTML = '';
            
            data.entries.forEach(entry => {
                const item = document.createElement('div');
                item.className = `file-item ${entry.isDirectory ? 'directory' : 'file'}`;
                item.innerHTML = `
                    <i class="material-icons">${entry.isDirectory ? 'folder' : 'description'}</i>
                    <span>${entry.name}</span>
                `;
                
                item.addEventListener('click', () => {
                    if (entry.isDirectory) {
                        this.loadFiles(entry.path);
                    } else {
                        this.openFile(entry.path);
                    }
                });
                
                fileList.appendChild(item);
            });
        } catch (error) {
            this.showNotification('Error loading files: ' + error.message, 'error');
        }
    }

    async openFile(path) {
        try {
            const response = await fetch(`/api/file/read?path=${encodeURIComponent(path)}`);
            if (!response.ok) throw new Error('Failed to read file');
            
            const data = await response.json();
            
            document.getElementById('editing-file').textContent = path;
            document.getElementById('file-content').value = data.content;
            document.getElementById('file-content').dataset.path = path;
            document.getElementById('file-editor').style.display = 'flex';
        } catch (error) {
            this.showNotification('Error opening file: ' + error.message, 'error');
        }
    }

    closeFileEditor() {
        document.getElementById('file-editor').style.display = 'none';
    }

    async saveFile() {
        const content = document.getElementById('file-content').value;
        const path = document.getElementById('file-content').dataset.path;
        
        try {
            const response = await fetch('/api/file/edit', {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json',
                },
                body: JSON.stringify({ path, content })
            });
            
            if (!response.ok) throw new Error('Failed to save file');
            
            const result = await response.json();
            if (result.applied) {
                this.showNotification('File saved successfully');
                this.closeFileEditor();
            } else {
                this.showNotification('Changes not applied', 'warning');
            }
        } catch (error) {
            this.showNotification('Error saving file: ' + error.message, 'error');
        }
    }

    // Configuration Management
    initializeConfigHandlers() {
        const saveButton = document.getElementById('save-config');
        const providerSelect = document.getElementById('provider-select');
        
        saveButton.addEventListener('click', () => this.saveConfiguration());
        providerSelect.addEventListener('change', (e) => {
            this.currentProvider = e.target.value;
            this.updateProviderDisplay();
        });
    }

    async loadConfiguration() {
        try {
            const response = await fetch('/api/configuration');
            if (!response.ok) throw new Error('Failed to load configuration');
            
            const config = await response.json();
            
            // Update form fields
            document.getElementById('provider-select').value = config.defaultProvider || 'openai';
            
            // OpenAI
            if (config.providers.openAI) {
                document.getElementById('openai-model').value = config.providers.openAI.model || 'gpt-4';
                if (config.providers.openAI.apiKeySet) {
                    document.getElementById('openai-api-key').placeholder = 'API Key is set';
                }
            }
            
            // Claude
            if (config.providers.claude) {
                document.getElementById('claude-model').value = config.providers.claude.model || 'claude-3-sonnet-20240229';
                if (config.providers.claude.apiKeySet) {
                    document.getElementById('claude-api-key').placeholder = 'API Key is set';
                }
            }
            
            // Ollama
            if (config.providers.ollama) {
                document.getElementById('ollama-base-url').value = config.providers.ollama.baseUrl || 'http://localhost:11434';
                document.getElementById('ollama-model').value = config.providers.ollama.model || 'llama2';
            }
            
            this.currentProvider = config.defaultProvider || 'openai';
            this.updateProviderDisplay();
        } catch (error) {
            this.showNotification('Error loading configuration: ' + error.message, 'error');
        }
    }

    async saveConfiguration() {
        const config = {
            defaultProvider: document.getElementById('provider-select').value,
            format: document.querySelector('input[name="format"]:checked').value
        };
        
        // OpenAI
        const openAIKey = document.getElementById('openai-api-key').value;
        const openAIModel = document.getElementById('openai-model').value;
        if (openAIKey || openAIModel) {
            config.openAI = {};
            if (openAIKey) config.openAI.apiKey = openAIKey;
            if (openAIModel) config.openAI.model = openAIModel;
        }
        
        // Claude
        const claudeKey = document.getElementById('claude-api-key').value;
        const claudeModel = document.getElementById('claude-model').value;
        if (claudeKey || claudeModel) {
            config.claude = {};
            if (claudeKey) config.claude.apiKey = claudeKey;
            if (claudeModel) config.claude.model = claudeModel;
        }
        
        // Ollama
        const ollamaUrl = document.getElementById('ollama-base-url').value;
        const ollamaModel = document.getElementById('ollama-model').value;
        if (ollamaUrl || ollamaModel) {
            config.ollama = {};
            if (ollamaUrl) config.ollama.baseUrl = ollamaUrl;
            if (ollamaModel) config.ollama.model = ollamaModel;
        }
        
        try {
            const response = await fetch('/api/configuration', {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json',
                },
                body: JSON.stringify(config)
            });
            
            if (!response.ok) throw new Error('Failed to save configuration');
            
            this.showNotification('Configuration saved successfully');
            
            // Clear sensitive fields
            document.getElementById('openai-api-key').value = '';
            document.getElementById('claude-api-key').value = '';
            
            // Reload configuration
            this.loadConfiguration();
        } catch (error) {
            this.showNotification('Error saving configuration: ' + error.message, 'error');
        }
    }

    updateProviderDisplay() {
        const indicator = document.getElementById('provider-indicator');
        const providerNames = {
            'openai': 'OpenAI',
            'claude': 'Claude',
            'ollama': 'Ollama'
        };
        indicator.textContent = providerNames[this.currentProvider] || this.currentProvider;
    }

    // Notifications
    showNotification(message, type = 'info') {
        const snackbar = document.getElementById('snackbar');
        const label = snackbar.querySelector('.mdc-snackbar__label');
        
        label.textContent = message;
        snackbar.classList.add('mdc-snackbar--open');
        
        setTimeout(() => {
            snackbar.classList.remove('mdc-snackbar--open');
        }, 3000);
    }
}

// Initialize app when DOM is ready
document.addEventListener('DOMContentLoaded', () => {
    window.app = new CodeAgentApp();
});