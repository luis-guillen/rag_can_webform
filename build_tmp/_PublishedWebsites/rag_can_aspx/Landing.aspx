<%@ Page Title="Inicio" Language="C#" MasterPageFile="~/Site.Master" AutoEventWireup="true" %>

<asp:Content ID="LandingContent" ContentPlaceHolderID="MainContent" runat="server">
    <div class="landing-page">
        <section class="landing-shell">
            <div class="landing-hero">
                <div class="landing-copy-block">
                    <div class="landing-kicker">Patrimonio, contexto y consulta</div>
                    <h1 class="landing-title">
                        Agente de Consulta del <span class="landing-title-accent">Patrimonio de Canarias</span>
                    </h1>
                    <p class="landing-copy">
                        Una entrada clara al ecosistema RAG de Canarias: consulta el conocimiento indexado,
                        sigue el hilo de las respuestas y accede a las fuentes utilizadas sin perder contexto.
                    </p>
                    <div class="landing-actions">
                        <a class="btn btn-primary btn-lg" runat="server" href="~/Chat.aspx">Entrar al chat</a>
                        <a class="btn btn-outline-light btn-lg" runat="server" href="~/Crawler">Ver crawler</a>
                    </div>
                </div>

                <div class="landing-panel">
                    <div class="landing-panel-title">Qué encontrarás</div>
                    <div class="landing-stat-grid">
                        <div class="landing-stat">
                            <span class="landing-stat-value">Respuestas trazables</span>
                            <div class="landing-stat-label">Cada respuesta puede mostrar las fuentes usadas para construirla.</div>
                        </div>
                        <div class="landing-stat">
                            <span class="landing-stat-value">Historial persistente</span>
                            <div class="landing-stat-label">Recupera conversaciones anteriores y continúa desde donde lo dejaste.</div>
                        </div>
                        <div class="landing-stat">
                            <span class="landing-stat-value">Diseño enfocado</span>
                            <div class="landing-stat-label">Una interfaz sobria, rápida y pensada para consultas largas.</div>
                        </div>
                        <div class="landing-stat">
                            <span class="landing-stat-value">Acceso directo</span>
                            <div class="landing-stat-label">Entra al chat principal o revisa el crawler cuando lo necesites.</div>
                        </div>
                    </div>
                </div>
            </div>

            <div class="landing-features">
                <div class="landing-feature">
                    <i class="fas fa-comments"></i>
                    <h3>Consulta guiada</h3>
                    <p>Explora el corpus con una experiencia de conversación limpia y centrada en la pregunta.</p>
                </div>
                <div class="landing-feature">
                    <i class="fas fa-file-lines"></i>
                    <h3>Fuentes visibles</h3>
                    <p>Las respuestas se apoyan en fuentes y contexto para que puedas verificar cada resultado.</p>
                </div>
                <div class="landing-feature">
                    <i class="fas fa-layer-group"></i>
                    <h3>Base escalable</h3>
                    <p>La portada deja preparado el salto al chat principal sin romper el resto de herramientas.</p>
                </div>
            </div>
        </section>
    </div>
</asp:Content>
