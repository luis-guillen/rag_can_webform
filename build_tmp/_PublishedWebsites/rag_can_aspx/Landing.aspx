<%@ Page Title="Inicio RAGCAN" Language="C#" MasterPageFile="~/Site.Master" AutoEventWireup="true" %>

<asp:Content ID="LandingContent" ContentPlaceHolderID="MainContent" runat="server">
    <div class="landing-page">
        <section class="landing-shell">
            <div class="landing-hero">
                <div class="landing-copy-block">
                    <div class="landing-kicker">Patrimonio, contexto y consulta</div>
                    <h1 class="landing-title">
                        Agente de Consulta del Patrimonio de Canarias
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
