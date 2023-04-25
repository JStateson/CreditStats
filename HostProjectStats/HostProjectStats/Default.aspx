﻿<%@ Page Language="C#" AutoEventWireup="true" CodeBehind="Default.aspx.cs" Inherits="HostProjectStats.Default" %>

<!DOCTYPE html>

<html xmlns="http://www.w3.org/1999/xhtml">
<head runat="server">
    <title>Statistics for BOINC project at a specified host</title>
</head>8
<body>

      

    <form id="form1" runat="server">
         <p> 

    <asp:Panel ID="Panel1" runat="server" Height="942px" Width="798px">
        Browse to a project and computer that interests you, select valid tasks at that computer, and<br /> make sure there are exactly 20 (or use a lower box value). Then copy the url from your browser<br /> into the &quot;Paste the url&quot; box below and click &quot;CALCULATE&quot;. You can also CLEAR the statistics<br /> or select additional pages of data up to a total of 10 pages.&nbsp;&nbsp; This program cannot log in to a users<br /> account so you must enter a url that points to a host computer and NOT a list of user tasks.<br /> To see the original data at web site click on &quot;REVIEW DATA&quot;.&nbsp; TEST DEMO may no longer work as<br /> projects block anon access due to EU laws.&nbsp; This program is useful on your own projects only.<br /> nCon is number of concurrent tasks in a single GPU (default is 1), nDev is Number of GPUs or CPUs<br /> To compute watts per credit, enter Idle Watts and Load Watts of the system.&nbsp; If hyperthreads are<br /> enabled use # of threads not 
        cores.&nbsp; If nCon &gt; 1 then raw stats are adjusted by dividing by nCon.<br /> YOU MUST SELECT A COMPUTER: not &quot;all tasks&quot; as this program CANNOT obtain &quot;?userid=&quot;<br /> TEST DEMO:
        <asp:DropDownList ID="ddlTest" runat="server" AutoPostBack="True" OnSelectedIndexChanged="ddlTest_SelectedIndexChanged">
            <asp:ListItem Value="https://milkyway.cs.rpi.edu/milkyway/results.php?hostid=776231&amp;offset=0&amp;show_names=0&amp;state=4&amp;appid=">Milkyway</asp:ListItem>
            <asp:ListItem Value="https://einsteinathome.org/host/10698787/tasks/4/0">Einstein</asp:ListItem>
            <asp:ListItem Value="https://boinc.thesonntags.com/collatz/results.php?hostid=833335&amp;offset=0&amp;show_names=0&amp;state=4&amp;appid=">Collatz</asp:ListItem>
            <asp:ListItem Value="http://www.gpugrid.net/results.php?hostid=467730&amp;offset=0&amp;show_names=0&amp;state=3&amp;appid=">GpuGrid</asp:ListItem>
            <asp:ListItem Value="https://sech.me/boinc/Amicable/results.php?hostid=33751&amp;offset=0&amp;show_names=0&amp;state=4&amp;appid=">Amicable</asp:ListItem>
            <asp:ListItem Value="https://asteroidsathome.net/boinc/results.php?hostid=732906&offset=0&show_names=0&state=4&appid=">Asteroids</asp:ListItem>
            <asp:ListItem Value="https://www.cosmologyathome.org/results.php?hostid=268457&amp;offset=0&amp;show_names=0&amp;state=4&amp;appid=">Cosmology</asp:ListItem>
            <asp:ListItem Value="http://www.enigmaathome.net/results.php?hostid=220602&amp;offset=0&amp;show_names=0&amp;state=4&amp;appid=">Enigma</asp:ListItem>
            <asp:ListItem Value="https://boinc.multi-pool.info/latinsquares/results.php?hostid=14170&amp;offset=0&amp;show_names=0&amp;state=4&amp;appid=">Odka1</asp:ListItem>
            <asp:ListItem Value="https://lhcathome.cern.ch/lhcathome/results.php?hostid=10587392&amp;offset=0&amp;show_names=0&amp;state=4&amp;appid=">LHC</asp:ListItem>
            <asp:ListItem Value="https://escatter11.fullerton.edu/nfs/results.php?hostid=880073&amp;offset=0&amp;show_names=0&amp;state=4&amp;appid=1">NFS</asp:ListItem>
            <asp:ListItem Value="http://pogs.theskynet.org/pogs/results.php?hostid=858228&amp;offset=0&amp;show_names=0&amp;state=4&amp;appid=">POGS</asp:ListItem>
            <asp:ListItem Value="https://setiathome.berkeley.edu/results.php?hostid=8619726&amp;offset=0&amp;show_names=0&amp;state=4&amp;appid=">SETI</asp:ListItem>
            <asp:ListItem>CreateQuery</asp:ListItem>
        </asp:DropDownList>
        &nbsp;&nbsp;&nbsp;&nbsp;&nbsp;
        <asp:Button ID="btnReview" runat="server" Text="REVIEW DATA" OnClick="btnReview_Click" Width="99px" OnClientClick="target ='_blank'" />
        &nbsp;&nbsp;&nbsp;&nbsp;Wu&nbsp;
        <asp:TextBox ID="tb_num2read" runat="server" Width="28px">20</asp:TextBox>
        &nbsp; &nbsp;&nbsp;&nbsp;<asp:Label ID="Label4" runat="server" Text="nCon"></asp:Label>
&nbsp;
        <asp:TextBox ID="tb_ntasks" runat="server" Width="28px">1</asp:TextBox>
        &nbsp;nDev
        <asp:TextBox ID="tb_ngpu" runat="server" Width="24px">1</asp:TextBox>
        <br />
        <br />
        <asp:Label ID="Label1" runat="server" BackColor="#FFCCFF" Text="Paste the url here"></asp:Label>
        :
        <asp:TextBox ID="ProjUrl" runat="server" Height="16px" Width="595px"></asp:TextBox>
        <br />
        <br />
        <asp:Button ID="btnCalc" runat="server" OnClick="btnCalc_Click" Text="CALCULATE" />
        &nbsp;&nbsp;&nbsp;&nbsp;
        <asp:Button ID="btnClear" runat="server" OnClick="btnClear_Click" Text="CLEAR" />
        &nbsp;&nbsp;&nbsp;&nbsp;
        <asp:Button ID="btnAbout" runat="server" OnClick="Button1_Click" Text="ABOUT" OnClientClick="target ='_blank'" />
        &nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;
        <asp:Label ID="Label2" runat="server" BackColor="#33CCCC" Text="Number of pages to gather"></asp:Label>
        &nbsp;
        <asp:DropDownList ID="ddlNumPages" runat="server" Width="44px">
            <asp:ListItem>1</asp:ListItem>
            <asp:ListItem>2</asp:ListItem>
            <asp:ListItem>3</asp:ListItem>
            <asp:ListItem>4</asp:ListItem>
            <asp:ListItem>5</asp:ListItem>
            <asp:ListItem>6</asp:ListItem>
            <asp:ListItem>7</asp:ListItem>
            <asp:ListItem>8</asp:ListItem>
            <asp:ListItem>9</asp:ListItem>
            <asp:ListItem>10</asp:ListItem>
        </asp:DropDownList>
        &nbsp;&nbsp;&nbsp;
        <asp:CheckBox ID="CBoxAdv" runat="server" Text="Auto Inc Url" />
        <br />
        <asp:Label ID="lblProjName" runat="server" Text="UNKNOWN PROJECT"></asp:Label>
        &nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;<asp:Label ID="Label5" runat="server" Text="Load"></asp:Label>
        &nbsp;Watts
        <asp:TextBox ID="tb_watts" runat="server" Width="39px" Height="17px">0</asp:TextBox>
        &nbsp;&nbsp;&nbsp;&nbsp;&nbsp; Idle Watts
        <asp:TextBox ID="tb_idle" runat="server" Width="33px">0</asp:TextBox>
        &nbsp;
        <asp:Button ID="btn_help" runat="server" OnClick="btn_help_Click" OnClientClick="target ='_blank'" Text="HELP" />
        &nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;
        <asp:CheckBox ID="CBoxNorm" runat="server" Text="Normalize Credit to 1" />
        <br />
        <asp:TextBox ID="ResultsBox" runat="server" Height="580px" ReadOnly="True" TextMode="MultiLine" Width="699px" ToolTip="sample location: https://milkyway.cs.rpi.edu/milkyway/results.php?hostid=766466&amp;offset=0&amp;show_names=0&amp;state=4&amp;appid="></asp:TextBox>





    </asp:Panel>

    </p>
        <div>
        </div>
    </form>
</body>
</html>
