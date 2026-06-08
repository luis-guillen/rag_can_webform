<%@ Page Title="Evaluación RAG" Language="C#" MasterPageFile="~/Site.Master"
    AutoEventWireup="true" CodeBehind="Evaluacion.aspx.cs"
    Inherits="rag_can_aspx.Evaluacion" %>

<asp:Content ID="BodyContent" ContentPlaceHolderID="MainContent" runat="server">
    <div class="container py-4">

        <h1 class="mb-2">
            <i class="fas fa-chart-line"></i> Evaluación del Sistema RAG
        </h1>
        <p class="lead text-muted mb-4">
            Métricas automáticas sobre las 50 preguntas del corpus de evaluación de patrimonio canario.
        </p>

        <!-- Panel de ejecución -->
        <div class="card mb-4">
            <div class="card-body">
                <h5 class="card-title"><i class="fas fa-play-circle"></i> Ejecutar evaluación</h5>

                <asp:UpdatePanel ID="updControl" runat="server" UpdateMode="Conditional">
                    <ContentTemplate>
                        <asp:Label ID="lblStatus" runat="server"
                            CssClass="alert alert-info d-block mb-3" Visible="false"></asp:Label>
                        <asp:Button ID="btnEvaluar" runat="server"
                            Text="Ejecutar evaluación completa (50 preguntas)"
                            CssClass="btn btn-primary"
                            OnClick="BtnEvaluar_Click" />
                        <small class="text-muted ms-3">
                            Lanza el runner Python en segundo plano. Puede tardar varios minutos
                            dependiendo de la latencia de la API.
                        </small>
                    </ContentTemplate>
                </asp:UpdatePanel>
            </div>
        </div>

        <!-- Panel de resultados con polling -->
        <asp:UpdatePanel ID="updResults" runat="server" UpdateMode="Conditional">
            <ContentTemplate>
                <asp:Timer ID="tmrPoll" runat="server" Interval="3000"
                    Enabled="false" OnTick="TmrPoll_Tick" />

                <!-- Progreso en tiempo real (visible mientras corre) -->
                <asp:PlaceHolder ID="phProgress" runat="server"></asp:PlaceHolder>

                <!-- Tarjetas de métricas -->
                <asp:PlaceHolder ID="phMetrics" runat="server"></asp:PlaceHolder>

                <!-- Tabla por categoría -->
                <asp:PlaceHolder ID="phCategoryTable" runat="server"></asp:PlaceHolder>

                <!-- Tabla por dificultad -->
                <asp:PlaceHolder ID="phDifficultyTable" runat="server"></asp:PlaceHolder>

                <!-- Accordion de resultados completos -->
                <asp:PlaceHolder ID="phFullResults" runat="server"></asp:PlaceHolder>
            </ContentTemplate>
        </asp:UpdatePanel>

    </div>
</asp:Content>
