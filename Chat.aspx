<%@ Page Title="Chat RAG" Language="C#" MasterPageFile="~/Site.Master" AutoEventWireup="true" CodeBehind="Chat.aspx.cs" Inherits="rag_can_aspx.Chat" %>

<asp:Content ID="ChatHead" ContentPlaceHolderID="MainContent" runat="server">
    <link href="https://fonts.googleapis.com/css2?family=DM+Sans:wght@400;500;600;700&family=Geist+Mono:wght@400;500&display=swap" rel="stylesheet">
    <style>
        * { box-sizing: border-box; }

        body {
            background: #0a0e27;
            color: #e0e0e0;
            font-family: 'DM Sans', -apple-system, BlinkMacSystemFont, sans-serif;
            font-size: 15px;
            line-height: 1.6;
        }

        #MainContent {
            display: flex;
            flex-direction: column;
            min-height: calc(100vh - 80px);
            padding: 0;
            margin: 0;
        }

        .chat-container {
            display: flex;
            flex: 1;
            overflow: hidden;
            min-height: 0;
        }

        .chat-sidebar {
            width: 280px;
            background: #0f1229;
            border-right: 1px solid rgba(255, 255, 255, 0.08);
            padding: 1.5rem 1rem;
            overflow-y: auto;
            flex-shrink: 0;
        }

        .sidebar-header {
            display: flex;
            align-items: center;
            gap: 0.75rem;
            margin-bottom: 2rem;
            padding-bottom: 1rem;
            border-bottom: 1px solid rgba(255, 255, 255, 0.08);
        }

        .sidebar-header-icon {
            width: 40px;
            height: 40px;
            border-radius: 12px;
            overflow: hidden;
            display: inline-flex;
            align-items: center;
            justify-content: center;
            flex-shrink: 0;
            border: 1px solid rgba(96, 165, 250, 0.22);
            background: linear-gradient(180deg, rgba(59, 130, 246, 0.18), rgba(255, 255, 255, 0.03));
            color: #dbeafe;
            font-size: 1rem;
        }

        .sidebar-header h2 {
            margin: 0;
            font-size: 17px;
            font-weight: 800;
            letter-spacing: -0.3px;
        }

        .history-new {
            width: 100%;
            padding: 0.75rem 0.9rem;
            margin-bottom: 1rem;
            border-radius: 8px;
            border: 1px solid rgba(96, 165, 250, 0.25);
            background: rgba(59, 130, 246, 0.12);
            color: #dbeafe;
            font-family: 'DM Sans', -apple-system, BlinkMacSystemFont, sans-serif;
            font-size: 14px;
            font-weight: 600;
            cursor: pointer;
            text-align: left;
            transition: all 0.2s ease;
        }

        .history-new:hover {
            background: rgba(59, 130, 246, 0.18);
            border-color: rgba(96, 165, 250, 0.4);
        }

        .history-list {
            display: flex;
            flex-direction: column;
            gap: 0.6rem;
        }

        .history-item {
            position: relative;
            padding: 0.75rem;
            border-radius: 8px;
            border: 1px solid rgba(255, 255, 255, 0.06);
            background: rgba(255, 255, 255, 0.03);
            transition: all 0.2s ease;
        }

        .history-item:hover,
        .history-item.active {
            background: rgba(59, 130, 246, 0.1);
            border-color: rgba(96, 165, 250, 0.25);
        }

        .history-open {
            display: block;
            color: #e5edf8;
            font-size: 13px;
            font-weight: 600;
            line-height: 1.35;
            text-decoration: none;
            overflow-wrap: anywhere;
            padding-right: 3.4rem;
        }

        .history-open:hover {
            color: #93c5fd;
            text-decoration: none;
        }

        .history-meta {
            margin-top: 0.35rem;
            color: #7c8ba1;
            font-family: 'Geist Mono', monospace;
            font-size: 11px;
        }

        .history-delete {
            position: absolute;
            top: 0.72rem;
            right: 0.65rem;
            color: #fca5a5;
            font-size: 11px;
            font-weight: 600;
            text-decoration: none;
        }

        .history-delete:hover {
            color: #fecaca;
            text-decoration: underline;
        }

        .history-empty {
            color: #707b8f;
            font-size: 12px;
            line-height: 1.4;
            text-align: center;
            padding: 2rem 0.5rem;
        }

        .chat-main {
            flex: 1;
            display: flex;
            flex-direction: column;
            background: #0a0e27;
        }

        .chat-header {
            padding: 1.5rem 2rem;
            border-bottom: 1px solid rgba(255, 255, 255, 0.08);
            background: linear-gradient(180deg, rgba(59, 130, 246, 0.05) 0%, transparent 100%);
        }

        .chat-header-content {
            max-width: 900px;
            margin: 0 auto;
        }

        .chat-header h1 {
            margin: 0 0 0.5rem 0;
            font-size: clamp(2rem, 3vw, 3.15rem);
            font-weight: 700;
            color: #f5f7fb;
        }

        .chat-header h1 .title-rag {
            color: #60a5fa;
        }

        .chat-header h1 .title-dark {
            color: #f5f7fb;
        }

        .chat-header p {
            margin: 0;
            color: #888;
            font-size: 15px;
        }

        .chat-messages {
            flex: 1;
            overflow-y: auto;
            padding: 2rem;
            display: flex;
            flex-direction: column;
            gap: 1.5rem;
            max-width: 900px;
            margin: 0 auto;
            width: 100%;
        }

        .chat-messages::-webkit-scrollbar {
            width: 8px;
        }

        .chat-messages::-webkit-scrollbar-track {
            background: transparent;
        }

        .chat-messages::-webkit-scrollbar-thumb {
            background: rgba(255, 255, 255, 0.1);
            border-radius: 4px;
        }

        .chat-messages::-webkit-scrollbar-thumb:hover {
            background: rgba(255, 255, 255, 0.15);
        }

        .chat-message {
            display: flex;
            gap: 1rem;
            animation: slideIn 0.3s ease-out;
        }

        @keyframes slideIn {
            from { opacity: 0; transform: translateY(10px); }
            to { opacity: 1; transform: translateY(0); }
        }

        .chat-message.user {
            flex-direction: row-reverse;
        }

        .chat-message-avatar {
            width: 32px;
            height: 32px;
            border-radius: 8px;
            display: flex;
            align-items: center;
            justify-content: center;
            font-size: 16px;
            flex-shrink: 0;
        }

        .chat-message.bot .chat-message-avatar {
            background: rgba(59, 130, 246, 0.15);
            color: #3b82f6;
        }

        .chat-message.user .chat-message-avatar {
            background: rgba(59, 130, 246, 0.25);
            color: #60a5fa;
        }

        .chat-message-content {
            flex: 1;
            display: flex;
            flex-direction: column;
            gap: 0.75rem;
            max-width: 600px;
        }

        .chat-bubble {
            padding: 1rem 1.25rem;
            border-radius: 12px;
            line-height: 1.6;
            white-space: pre-wrap;
            word-wrap: break-word;
        }

        .chat-message.bot .chat-bubble {
            background: rgba(59, 130, 246, 0.08);
            border: 1px solid rgba(59, 130, 246, 0.15);
            color: #e0e0e0;
        }

        .chat-message.user .chat-bubble {
            background: linear-gradient(135deg, #3b82f6 0%, #2563eb 100%);
            color: #fff;
        }

        .chat-bubble em {
            color: #888;
            font-style: italic;
        }

        .chat-sources {
            margin-top: 0.75rem;
            padding: 1rem;
            background: rgba(59, 130, 246, 0.06);
            border: 1px solid rgba(59, 130, 246, 0.12);
            border-radius: 10px;
            font-size: 13px;
        }

        .chat-sources-title {
            font-weight: 600;
            margin-bottom: 0.75rem;
            color: #60a5fa;
            display: flex;
            align-items: center;
            gap: 0.5rem;
        }

        .chat-sources-list {
            list-style: none;
            padding: 0;
            margin: 0;
            display: flex;
            flex-direction: column;
            gap: 0.5rem;
        }

        .chat-sources-list li {
            padding: 0.5rem 0.75rem;
            background: rgba(255, 255, 255, 0.03);
            border-radius: 6px;
            border: 1px solid rgba(255, 255, 255, 0.05);
            transition: all 0.2s ease;
        }

        .chat-sources-list li:hover {
            background: rgba(59, 130, 246, 0.12);
            border-color: rgba(59, 130, 246, 0.3);
        }

        .chat-sources-list a {
            color: #60a5fa;
            text-decoration: none;
            font-weight: 500;
            transition: color 0.2s ease;
        }

        .chat-sources-list a:hover {
            color: #93c5fd;
            text-decoration: underline;
        }

        .chat-sources-badge {
            display: inline-block;
            background: rgba(59, 130, 246, 0.15);
            color: #60a5fa;
            padding: 0.2rem 0.6rem;
            border-radius: 4px;
            font-size: 12px;
            font-weight: 500;
            margin-left: 0.5rem;
        }

        .chat-answer-mode {
            color: #8aa4c7;
            font-size: 12px;
            font-family: 'Geist Mono', monospace;
            margin-left: 0.25rem;
        }

        .chat-health {
            display: flex;
            flex-wrap: wrap;
            gap: 0.5rem;
            margin-top: 0.75rem;
        }

        .health-pill {
            display: inline-flex;
            align-items: center;
            min-height: 24px;
            padding: 0.2rem 0.55rem;
            border-radius: 4px;
            background: rgba(255, 255, 255, 0.06);
            border: 1px solid rgba(255, 255, 255, 0.08);
            color: #b7c5d8;
            font-family: 'Geist Mono', monospace;
            font-size: 12px;
            overflow-wrap: anywhere;
        }

        .health-ok {
            color: #86efac;
            border-color: rgba(34, 197, 94, 0.25);
            background: rgba(34, 197, 94, 0.08);
        }

        .health-warn {
            color: #fca5a5;
            border-color: rgba(239, 68, 68, 0.25);
            background: rgba(239, 68, 68, 0.08);
        }

        .chat-empty {
            display: flex;
            flex-direction: column;
            align-items: center;
            justify-content: center;
            gap: 1rem;
            color: #666;
            padding: 2rem;
            text-align: center;
            flex: 1;
        }

        .chat-empty-icon {
            font-size: 48px;
            opacity: 0.5;
        }

        .chat-error {
            padding: 0.625rem 1rem;
            background: rgba(239, 68, 68, 0.08);
            border: 1px solid rgba(239, 68, 68, 0.2);
            border-radius: 8px;
            color: #ff9999;
            font-size: 12px;
            margin-bottom: 0.875rem;
            line-height: 1.4;
        }

        .chat-footer {
            padding: 1.5rem 2rem;
            border-top: 1px solid rgba(255, 255, 255, 0.08);
            background: linear-gradient(180deg, transparent 0%, rgba(59, 130, 246, 0.03) 100%);
        }

        .chat-footer-content {
            max-width: 900px;
            margin: 0 auto;
            width: 100%;
        }

        .input-group-wrapper {
            display: flex;
            gap: 1rem;
            margin-bottom: 0.75rem;
            flex-wrap: wrap;
            align-items: flex-start;
        }

        .input-group-wrapper input {
            flex: 1;
            min-width: 200px;
            padding: 0.875rem 1.25rem;
            border: 1px solid rgba(255, 255, 255, 0.12);
            border-radius: 10px;
            background: rgba(255, 255, 255, 0.05);
            color: #fff;
            font-family: 'DM Sans', -apple-system, BlinkMacSystemFont, sans-serif;
            font-size: 15px;
            transition: all 0.2s ease;
        }

        .input-group-wrapper input:focus {
            outline: none;
            border-color: #3b82f6;
            background: rgba(255, 255, 255, 0.08);
            box-shadow: 0 0 0 3px rgba(59, 130, 246, 0.1);
        }

        .input-group-wrapper input::placeholder {
            color: #666;
        }

        .btn-group {
            display: flex;
            gap: 0.75rem;
        }

        .btn {
            padding: 0.875rem 1.5rem;
            border: none;
            border-radius: 10px;
            font-family: 'DM Sans', -apple-system, BlinkMacSystemFont, sans-serif;
            font-size: 15px;
            font-weight: 600;
            cursor: pointer;
            transition: all 0.2s ease;
            text-transform: capitalize;
            white-space: nowrap;
        }

        .btn-send {
            background: linear-gradient(135deg, #3b82f6 0%, #2563eb 100%);
            color: #fff;
            min-width: 90px;
        }

        .btn-send:hover {
            box-shadow: 0 4px 12px rgba(59, 130, 246, 0.4);
            transform: translateY(-1px);
        }

        .btn-send:active {
            transform: translateY(0);
        }

        .btn-clear {
            background: rgba(255, 255, 255, 0.08);
            color: #e0e0e0;
            border: 1px solid rgba(255, 255, 255, 0.12);
            min-width: 90px;
        }

        .btn-clear:hover {
            background: rgba(255, 255, 255, 0.12);
            border-color: rgba(255, 255, 255, 0.2);
        }

        .chat-footer-info {
            font-size: 12px;
            color: #666;
            display: flex;
            align-items: center;
            gap: 0.5rem;
        }

        .chat-footer-info code {
            background: rgba(255, 255, 255, 0.05);
            padding: 0.25rem 0.5rem;
            border-radius: 4px;
            font-family: 'Geist Mono', monospace;
            color: #60a5fa;
        }

        @media (max-width: 1024px) {
            .chat-sidebar { width: 240px; }
        }

        @media (max-width: 768px) {
            .chat-sidebar { display: none; }
            .chat-container { flex-direction: column; }
            .chat-header { padding: 1rem; }
            .chat-messages { padding: 1rem; gap: 1rem; max-width: 100%; }
            .chat-footer { padding: 1rem; }
            .chat-footer-content { max-width: 100%; }
            .input-group-wrapper { flex-direction: column; }
            .btn-group { width: 100%; }
            .btn-group .btn { flex: 1; }
        }
    </style>

    <div class="chat-container">
        <div class="chat-sidebar">
            <div class="sidebar-header">
                <div class="sidebar-header-icon" aria-hidden="true">
                    <i class="fas fa-clock-rotate-left"></i>
                </div>
                <h2>Historial</h2>
            </div>
            <asp:Button ID="btnNuevoChat" runat="server" Text="Nuevo chat"
                CssClass="history-new" OnClick="BtnNuevoChat_Click"
                CausesValidation="false" />
            <asp:PlaceHolder ID="phHistory" runat="server"></asp:PlaceHolder>
        </div>

            <div class="chat-main">
                <div class="chat-header">
                    <div class="chat-header-content">
                    <h1><span class="title-rag">RAG</span> Chat</h1>
                    <p>Haz preguntas sobre el corpus indexado y recibe respuestas con contexto y fuentes.</p>
                    </div>
                </div>

            <div class="chat-messages" id="chatWindow">
                <asp:Literal ID="litConversacion" runat="server" Mode="PassThrough"></asp:Literal>
            </div>

            <div class="chat-footer">
                <div class="chat-footer-content">
                    <asp:Label ID="lblError" runat="server" CssClass="chat-error"
                        Visible="false"></asp:Label>

                    <div class="input-group-wrapper">
                        <asp:TextBox ID="txtPregunta" runat="server"
                            Placeholder="Escribe tu pregunta..." autocomplete="off" />
                        <div class="btn-group">
                            <asp:Button ID="btnEnviar" runat="server" Text="Enviar"
                                CssClass="btn btn-send" OnClick="BtnEnviar_Click" />
                            <asp:Button ID="btnLimpiar" runat="server" Text="Limpiar"
                                CssClass="btn btn-clear" OnClick="BtnLimpiar_Click"
                                CausesValidation="false" />
                        </div>
                    </div>

                    <div class="chat-health">
                        <asp:Literal ID="litHealth" runat="server" Mode="PassThrough"></asp:Literal>
                    </div>
                </div>
            </div>
        </div>
    </div>
</asp:Content>

<asp:Content ID="ChatScripts" ContentPlaceHolderID="PageScripts" runat="server">
    <script>
        (function () {
            function scrollToBottom() {
                var w = document.getElementById('chatWindow');
                if (w) {
                    setTimeout(function () {
                        w.scrollTop = w.scrollHeight;
                    }, 50);
                }
            }

            function focusInput() {
                var box = document.getElementById('<%= txtPregunta.ClientID %>');
                if (box) box.focus();
            }

            scrollToBottom();
            focusInput();

            // Re-render sources with proper HTML structure
            var sourceItems = document.querySelectorAll('.chat-sources');
            sourceItems.forEach(function (el) {
                var html = el.innerHTML;
                // Parse and rebuild sources if needed
            });
        })();
    </script>
</asp:Content>
