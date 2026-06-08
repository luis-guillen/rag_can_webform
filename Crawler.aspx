<%@ Page Title="Crawler RAGCAN" Language="C#" MasterPageFile="~/Site.Master" AutoEventWireup="true" CodeBehind="Crawler.aspx.cs" Inherits="rag_can_aspx.Crawler" %>

<asp:Content ID="BodyContent" ContentPlaceHolderID="MainContent" runat="server">
    <div class="container crawler-page">
        <h1 class="mb-3">Crawler <span class="title-rag">RAG</span><span class="title-dark">CAN</span></h1>
        <p class="lead">
            Lanza el crawling en segundo plano. Puedes cerrar o cambiar de pagina: el job sigue
            y el estado se persiste en <code>App_Data/status</code>.
        </p>

        <!-- Panel de control -->
        <div class="card mb-4">
            <div class="card-body">
                <div class="mb-3">
                    <label for="txtUrl" class="form-label"><i class="fas fa-link"></i> URL unica (opcional):</label>
                    <asp:TextBox ID="txtUrl" runat="server" CssClass="form-control" Placeholder="https://ejemplo.com (vacio = usar seeds.txt)" />
                </div>
                <div class="mb-3">
                    <label for="fuSeeds" class="form-label"><i class="fas fa-file-lines"></i> Archivo de URLs (.txt):</label>
                    <asp:FileUpload ID="fuSeeds" runat="server" CssClass="form-control file-upload-wide" />
                    <div class="form-text">Una URL por linea. Las lineas vacias y las que empiezan por # se ignoran.</div>
                </div>
                <div class="mb-3">
                    <label class="form-label d-block">Modo del archivo:</label>
                    <asp:RadioButtonList ID="rblSeedFileMode" runat="server" CssClass="form-check" RepeatDirection="Vertical">
                        <asp:ListItem Value="crawlOnly" Selected="True">Usar solo este crawl</asp:ListItem>
                        <asp:ListItem Value="saveAndUse">Guardar como semillas y usar</asp:ListItem>
                    </asp:RadioButtonList>
                </div>
                <div class="row">
                    <div class="col-md-6 mb-3">
                        <label for="txtMaxPages" class="form-label">Max Paginas:</label>
                        <asp:TextBox ID="txtMaxPages" runat="server" CssClass="form-control" TextMode="Number" Text="50" />
                    </div>
                    <div class="col-md-6 mb-3">
                        <label for="txtMaxDepth" class="form-label">Max Profundidad:</label>
                        <asp:TextBox ID="txtMaxDepth" runat="server" CssClass="form-control" TextMode="Number" Text="2" />
                    </div>
                </div>

                <asp:UpdatePanel ID="updControl" runat="server" UpdateMode="Conditional">
                    <ContentTemplate>
                        <asp:Label ID="lblMensaje" runat="server" CssClass="alert alert-info d-block mb-3" Visible="false"></asp:Label>
                        <div class="crawler-actions">
                            <asp:Button ID="btnIniciar" runat="server" Text="Iniciar Crawling"
                                CssClass="btn btn-primary btn-lg" OnClick="BtnIniciar_Click" />
                            <asp:Button ID="btnParar" runat="server" Text="Parar"
                                CssClass="btn btn-outline-danger btn-lg" OnClick="BtnParar_Click" />
                        </div>
                    </ContentTemplate>
                    <Triggers>
                        <asp:PostBackTrigger ControlID="btnIniciar" />
                    </Triggers>
                </asp:UpdatePanel>
            </div>
        </div>

        <!-- Estado en vivo -->
        <asp:UpdatePanel ID="updEstado" runat="server" UpdateMode="Conditional">
            <ContentTemplate>
                <asp:Timer ID="tmrRefresco" runat="server" Interval="3000" OnTick="TmrRefresco_Tick" />

                <div class="card mb-4">
                    <div class="card-body">
                        <h4 class="mb-3"><i class="fas fa-gauge-high"></i> Estado del crawl</h4>
                        <asp:Literal ID="litEstado" runat="server" Mode="PassThrough"></asp:Literal>
                    </div>
                </div>

                <div class="card mb-4">
                    <div class="card-body">
                        <h4 class="mb-3"><i class="fas fa-list-check"></i> Fuentes</h4>
                        <asp:Literal ID="litFuentes" runat="server" Mode="PassThrough"></asp:Literal>
                    </div>
                </div>

                <div class="card mb-4">
                    <div class="card-body">
                        <h4 class="mb-3"><i class="fas fa-terminal"></i> Logs recientes (crawler.log)</h4>
                        <asp:Literal ID="litLogs" runat="server" Mode="PassThrough"></asp:Literal>
                    </div>
                </div>
            </ContentTemplate>
        </asp:UpdatePanel>

        <!-- Scheduler -->
        <div class="card mb-4">
            <div class="card-body">
                <h4 class="mb-3"><i class="fas fa-clock"></i> Programacion (Scheduler)</h4>
                <div class="row">
                    <div class="col-md-4 mb-3">
                        <label for="ddlMode" class="form-label">Modo:</label>
                        <asp:DropDownList ID="ddlMode" runat="server" CssClass="form-select">
                            <asp:ListItem Value="manual" Text="Manual (sin programacion)" />
                            <asp:ListItem Value="interval" Text="Cada X horas" />
                            <asp:ListItem Value="daily" Text="Diario a una hora" />
                        </asp:DropDownList>
                    </div>
                    <div class="col-md-4 mb-3">
                        <label for="txtIntervalHours" class="form-label">Intervalo (horas):</label>
                        <asp:TextBox ID="txtIntervalHours" runat="server" CssClass="form-control" TextMode="Number" Text="24" />
                    </div>
                    <div class="col-md-4 mb-3">
                        <label for="txtDailyTime" class="form-label">Hora diaria (HH:mm):</label>
                        <asp:TextBox ID="txtDailyTime" runat="server" CssClass="form-control" Text="03:00" />
                    </div>
                </div>
                <div class="form-check">
                    <asp:CheckBox ID="chkSchedCrawl" runat="server" CssClass="form-check-input" Checked="true" />
                    <label class="form-check-label" for="chkSchedCrawl">Ejecutar crawl programado</label>
                </div>
                <div class="form-check mb-3">
                    <asp:CheckBox ID="chkSchedIndex" runat="server" CssClass="form-check-input" Checked="true" />
                    <label class="form-check-label" for="chkSchedIndex">Ejecutar indexado tras el crawl</label>
                </div>
                <asp:Label ID="lblScheduler" runat="server" CssClass="alert alert-success d-block mb-3" Visible="false"></asp:Label>
                <div class="crawler-actions crawler-actions-secondary">
                    <asp:Button ID="btnGuardarScheduler" runat="server" Text="Guardar programacion"
                        CssClass="btn btn-secondary" OnClick="BtnGuardarScheduler_Click" />
                </div>
            </div>
        </div>
    </div>
</asp:Content>
