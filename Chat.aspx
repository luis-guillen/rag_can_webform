<%@ Page Title="Chat RAG" Language="C#" MasterPageFile="~/Site.Master" AutoEventWireup="true" CodeBehind="Chat.aspx.cs" Inherits="rag_can_aspx.Chat" %>

<asp:Content ID="ChatHead" ContentPlaceHolderID="MainContent" runat="server">
    <link href="https://fonts.googleapis.com/css2?family=DM+Sans:wght@400;500;600;700&family=Geist+Mono:wght@400;500&display=swap" rel="stylesheet">
    <style>
        * { box-sizing: border-box; }

        body {
            background: var(--bg);
            color: var(--text);
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

        .chat-message.is-thinking {
            opacity: 0;
            transform: translateY(12px);
            pointer-events: none;
            max-height: 0;
            overflow: hidden;
            transition: opacity 0.2s ease, transform 0.2s ease, max-height 0.2s ease, margin 0.2s ease;
            margin: 0;
        }

        .chat-message.is-thinking.visible {
            opacity: 1;
            transform: translateY(0);
            max-height: 220px;
            margin-top: 0.25rem;
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

        .chat-message-avatar i {
            font-size: 14px;
            line-height: 1;
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

        .chat-message.is-thinking .chat-bubble {
            background: linear-gradient(180deg, rgba(18, 25, 58, 0.92) 0%, rgba(14, 20, 46, 0.92) 100%);
            border: 1px solid rgba(96, 165, 250, 0.18);
            box-shadow: 0 12px 30px rgba(5, 10, 30, 0.28);
        }

        .chat-message.user .chat-bubble {
            background: linear-gradient(135deg, #3b82f6 0%, #2563eb 100%);
            color: #fff;
        }

        .chat-bubble em {
            color: #888;
            font-style: italic;
        }

        .thinking-bubble {
            display: flex;
            align-items: center;
            gap: 0.9rem;
        }

        .thinking-pulse {
            display: inline-flex;
            align-items: center;
            gap: 0.35rem;
        }

        .thinking-pulse span {
            width: 9px;
            height: 9px;
            border-radius: 999px;
            background: linear-gradient(180deg, #93c5fd 0%, #3b82f6 100%);
            box-shadow: 0 0 0 1px rgba(147, 197, 253, 0.08);
            animation: thinkingPulse 1s ease-in-out infinite;
        }

        .thinking-pulse span:nth-child(2) {
            animation-delay: 0.16s;
        }

        .thinking-pulse span:nth-child(3) {
            animation-delay: 0.32s;
        }

        .thinking-copy {
            display: flex;
            flex-direction: column;
            gap: 0.2rem;
        }

        .thinking-title {
            color: #eef4ff;
            font-size: 14px;
            font-weight: 700;
            letter-spacing: 0.01em;
        }

        .thinking-subtitle {
            color: #8fa5c8;
            font-size: 12px;
        }

        @keyframes thinkingPulse {
            0%, 80%, 100% {
                transform: translateY(0) scale(0.92);
                opacity: 0.45;
            }

            40% {
                transform: translateY(-2px) scale(1);
                opacity: 1;
            }
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
            color: #7fa8ff;
        }

        .chat-empty-icon i {
            font-size: 44px;
            line-height: 1;
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
            max-width: none;
            margin: 0;
            width: 100%;
        }

        .input-group-wrapper {
            display: flex;
            gap: 0.6rem;
            margin-bottom: 0.75rem;
            align-items: flex-end;
            width: 100%;
        }

        .input-group-wrapper .chat-prompt {
            flex: 1;
            width: 100%;
            max-width: none;
            min-width: 0;
            min-height: 56px;
            max-height: 220px;
            padding: 0.875rem 1.25rem;
            border: 1px solid rgba(255, 255, 255, 0.12);
            border-radius: 10px;
            background: rgba(255, 255, 255, 0.05);
            color: #fff;
            font-family: 'DM Sans', -apple-system, BlinkMacSystemFont, sans-serif;
            font-size: 15px;
            line-height: 1.5;
            transition: all 0.2s ease;
            resize: none;
            overflow-y: auto;
        }

        .input-group-wrapper .chat-prompt:focus {
            outline: none;
            border-color: #3b82f6;
            background: rgba(255, 255, 255, 0.08);
            box-shadow: 0 0 0 3px rgba(59, 130, 246, 0.1);
        }

        .input-group-wrapper .chat-prompt::placeholder {
            color: #666;
        }

        .chat-container .btn-group {
            display: flex;
            flex: 0 0 auto;
            align-self: stretch;
        }

        .chat-container .btn {
            height: 56px;
            padding: 0 1.15rem;
            border: none;
            border-radius: 10px;
            font-family: 'DM Sans', -apple-system, BlinkMacSystemFont, sans-serif;
            font-size: 14px;
            font-weight: 600;
            cursor: pointer;
            transition: all 0.2s ease;
            text-transform: capitalize;
            white-space: nowrap;
        }

        .btn-send {
            background: linear-gradient(135deg, #3b82f6 0%, #2563eb 100%);
            color: #fff;
            min-width: 84px;
        }

        .btn-send:hover {
            box-shadow: 0 4px 12px rgba(59, 130, 246, 0.4);
            transform: translateY(-1px);
        }

        .btn-send:active {
            transform: translateY(0);
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

        html:not(.dark-mode) .chat-container {
            color: #1f2937;
        }

        html:not(.dark-mode) .chat-sidebar {
            background: #f8fafc;
            border-right-color: #e2e8f0;
        }

        html:not(.dark-mode) .sidebar-header {
            border-bottom-color: #e2e8f0;
        }

        html:not(.dark-mode) .sidebar-header-icon {
            border-color: rgba(13, 110, 253, 0.24);
            background: linear-gradient(180deg, rgba(13, 110, 253, 0.12), rgba(13, 110, 253, 0.03));
            color: #0d6efd;
        }

        html:not(.dark-mode) .history-new {
            border-color: rgba(13, 110, 253, 0.24);
            background: rgba(13, 110, 253, 0.08);
            color: #0b5ed7;
        }

        html:not(.dark-mode) .history-new:hover {
            background: rgba(13, 110, 253, 0.13);
            border-color: rgba(13, 110, 253, 0.35);
        }

        html:not(.dark-mode) .history-item {
            background: #ffffff;
            border-color: #e5e7eb;
        }

        html:not(.dark-mode) .history-item:hover,
        html:not(.dark-mode) .history-item.active {
            background: #eff6ff;
            border-color: rgba(13, 110, 253, 0.24);
        }

        html:not(.dark-mode) .history-open {
            color: #1f2937;
        }

        html:not(.dark-mode) .history-open:hover,
        html:not(.dark-mode) .chat-sources-list a:hover {
            color: #0b5ed7;
        }

        html:not(.dark-mode) .history-meta,
        html:not(.dark-mode) .history-empty,
        html:not(.dark-mode) .chat-header p,
        html:not(.dark-mode) .chat-bubble em,
        html:not(.dark-mode) .chat-empty,
        html:not(.dark-mode) .chat-footer-info,
        html:not(.dark-mode) .chat-answer-mode,
        html:not(.dark-mode) .thinking-subtitle {
            color: #64748b;
        }

        html:not(.dark-mode) .chat-main {
            background: #ffffff;
        }

        html:not(.dark-mode) .chat-header {
            background: linear-gradient(180deg, rgba(13, 110, 253, 0.04) 0%, transparent 100%);
            border-bottom-color: #e5e7eb;
        }

        html:not(.dark-mode) .chat-header h1,
        html:not(.dark-mode) .chat-header h1 .title-dark,
        html:not(.dark-mode) .thinking-title {
            color: #111827;
        }

        html:not(.dark-mode) .chat-messages::-webkit-scrollbar-thumb {
            background: rgba(15, 23, 42, 0.16);
        }

        html:not(.dark-mode) .chat-messages::-webkit-scrollbar-thumb:hover {
            background: rgba(15, 23, 42, 0.24);
        }

        html:not(.dark-mode) .chat-message.bot .chat-bubble,
        html:not(.dark-mode) .chat-message.is-thinking .chat-bubble {
            background: #f8fafc;
            border-color: #dbeafe;
            color: #1f2937;
            box-shadow: none;
        }

        html:not(.dark-mode) .chat-sources {
            background: #f8fbff;
            border-color: #dbeafe;
        }

        html:not(.dark-mode) .chat-sources-title,
        html:not(.dark-mode) .chat-sources-list a,
        html:not(.dark-mode) .chat-sources-badge,
        html:not(.dark-mode) .chat-footer-info code {
            color: #0d6efd;
        }

        html:not(.dark-mode) .chat-sources-list li {
            background: #ffffff;
            border-color: #e5e7eb;
        }

        html:not(.dark-mode) .chat-sources-list li:hover {
            background: #eff6ff;
            border-color: rgba(13, 110, 253, 0.25);
        }

        html:not(.dark-mode) .chat-sources-badge,
        html:not(.dark-mode) .chat-footer-info code {
            background: rgba(13, 110, 253, 0.08);
        }

        html:not(.dark-mode) .health-pill {
            background: #f8fafc;
            border-color: #e5e7eb;
            color: #475569;
        }

        html:not(.dark-mode) .health-ok {
            background: rgba(25, 135, 84, 0.08);
            border-color: rgba(25, 135, 84, 0.25);
            color: #147246;
        }

        html:not(.dark-mode) .health-warn {
            background: rgba(220, 53, 69, 0.08);
            border-color: rgba(220, 53, 69, 0.25);
            color: #b02a37;
        }

        html:not(.dark-mode) .chat-error {
            background: rgba(220, 53, 69, 0.08);
            border-color: rgba(220, 53, 69, 0.22);
            color: #b02a37;
        }

        html:not(.dark-mode) .chat-footer {
            background: linear-gradient(180deg, transparent 0%, rgba(13, 110, 253, 0.03) 100%);
            border-top-color: #e5e7eb;
        }

        html:not(.dark-mode) .input-group-wrapper .chat-prompt {
            background: #ffffff;
            border-color: #cbd5e1;
            color: #111827;
        }

        html:not(.dark-mode) .input-group-wrapper .chat-prompt:focus {
            background: #ffffff;
            border-color: #0d6efd;
            box-shadow: 0 0 0 3px rgba(13, 110, 253, 0.12);
        }

        html:not(.dark-mode) .input-group-wrapper .chat-prompt::placeholder {
            color: #94a3b8;
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
            .input-group-wrapper { gap: 0.75rem; }
            .input-group-wrapper .chat-prompt { min-height: 52px; }
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
                <div class="chat-message bot is-thinking" id="chatThinking" aria-live="polite" aria-hidden="true">
                    <div class="chat-message-avatar" aria-hidden="true">
                        <i class="fas fa-robot"></i>
                    </div>
                    <div class="chat-message-content">
                        <div class="chat-bubble thinking-bubble">
                            <div class="thinking-pulse" aria-hidden="true">
                                <span></span>
                                <span></span>
                                <span></span>
                            </div>
                            <div class="thinking-copy">
                                <div class="thinking-title">Pensando</div>
                                <div class="thinking-subtitle">Preparando la respuesta y revisando el contexto.</div>
                            </div>
                        </div>
                    </div>
                </div>
            </div>

            <div class="chat-footer">
                <div class="chat-footer-content">
                    <asp:Label ID="lblError" runat="server" CssClass="chat-error"
                        Visible="false"></asp:Label>

                    <div class="input-group-wrapper">
                        <asp:TextBox ID="txtPregunta" runat="server"
                            CssClass="chat-prompt"
                            TextMode="MultiLine" Rows="1"
                            onkeydown="return handlePromptKey(event);"
                            Placeholder="Escribe tu pregunta..." autocomplete="off" />
                        <div class="btn-group">
                            <asp:Button ID="btnEnviar" runat="server" Text="Enviar"
                                CssClass="btn btn-send" OnClick="BtnEnviar_Click"
                                UseSubmitBehavior="false" />
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
        function handlePromptKey(event) {
            if (event.key === 'Enter' && !event.shiftKey) {
                event.preventDefault();
                submitPrompt();
                return false;
            }

            return true;
        }

        (function () {
            var isSubmitting = false;

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

            function autoResizeInput() {
                var box = document.getElementById('<%= txtPregunta.ClientID %>');
                if (!box) return;
                box.style.height = 'auto';
                box.style.height = Math.min(box.scrollHeight, 220) + 'px';
            }

            function wirePromptSubmit() {
                var box = document.getElementById('<%= txtPregunta.ClientID %>');
                var send = document.getElementById('<%= btnEnviar.ClientID %>');
                if (!box || !send) return;

                autoResizeInput();

                box.addEventListener('input', autoResizeInput);
                send.addEventListener('click', function () {
                    if (!showThinkingState()) {
                        return false;
                    }

                    return true;
                });
            }

            function showThinkingState() {
                var box = document.getElementById('<%= txtPregunta.ClientID %>');
                var send = document.getElementById('<%= btnEnviar.ClientID %>');
                var thinking = document.getElementById('chatThinking');
                var prompt = box ? box.value.trim() : '';

                if (!prompt || isSubmitting) {
                    return false;
                }

                isSubmitting = true;

                if (box) {
                    box.setAttribute('readonly', 'readonly');
                }

                if (send) {
                    send.disabled = true;
                    send.textContent = '...';
                }

                if (thinking) {
                    thinking.classList.add('visible');
                    thinking.setAttribute('aria-hidden', 'false');
                }

                scrollToBottom();
                return true;
            }

            scrollToBottom();
            focusInput();
            wirePromptSubmit();

            // Re-render sources with proper HTML structure
            var sourceItems = document.querySelectorAll('.chat-sources');
            sourceItems.forEach(function (el) {
                var html = el.innerHTML;
                // Parse and rebuild sources if needed
            });
        })();

        function submitPrompt() {
            if (typeof __doPostBack !== 'function') {
                return;
            }

            var box = document.getElementById('<%= txtPregunta.ClientID %>');
            var thinking = document.getElementById('chatThinking');

            if (!box || !box.value.trim()) {
                return;
            }

            if (thinking) {
                thinking.classList.add('visible');
                thinking.setAttribute('aria-hidden', 'false');
            }

            setTimeout(function () {
                __doPostBack('<%= btnEnviar.UniqueID %>', '');
            }, 40);
        }
    </script>
</asp:Content>
