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
                <p class="text-muted small mb-3">
                    Lanza el runner Python en segundo plano (50 preguntas, ~3 min).
                    Usa el LLM activo en la Demo API al momento de lanzarlo.
                </p>

                <asp:UpdatePanel ID="updControl" runat="server" UpdateMode="Conditional">
                    <ContentTemplate>
                        <asp:Label ID="lblStatus" runat="server"
                            CssClass="alert alert-info d-block mb-3" Visible="false"></asp:Label>
                        <asp:Button ID="btnEvaluarRemoto" runat="server"
                            Text="Evaluar — LLM remoto (Dell Pro Max)"
                            CssClass="btn btn-primary me-2"
                            OnClick="BtnEvaluarRemoto_Click" />
                        <asp:Button ID="btnEvaluarLocal" runat="server"
                            Text="Evaluar — LLM local (Ollama)"
                            CssClass="btn btn-outline-secondary"
                            OnClick="BtnEvaluarLocal_Click" />
                    </ContentTemplate>
                </asp:UpdatePanel>
            </div>
        </div>

        <!-- Panel de resultados con polling -->
        <asp:UpdatePanel ID="updResults" runat="server" UpdateMode="Conditional">
            <ContentTemplate>
                <asp:Timer ID="tmrPoll" runat="server" Interval="3000"
                    Enabled="false" OnTick="TmrPoll_Tick" />

                <!-- Progreso en tiempo real -->
                <asp:PlaceHolder ID="phProgress" runat="server"></asp:PlaceHolder>

                <!-- Tabs remoto / local -->
                <asp:PlaceHolder ID="phTabs" runat="server"></asp:PlaceHolder>

                <!-- Contenido del tab activo -->
                <asp:PlaceHolder ID="phMetrics" runat="server"></asp:PlaceHolder>
                <asp:PlaceHolder ID="phCategoryTable" runat="server"></asp:PlaceHolder>
                <asp:PlaceHolder ID="phDifficultyTable" runat="server"></asp:PlaceHolder>
                <asp:PlaceHolder ID="phFullResults" runat="server"></asp:PlaceHolder>
            </ContentTemplate>
        </asp:UpdatePanel>

    </div>
</asp:Content>
