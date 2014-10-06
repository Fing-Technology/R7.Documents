﻿<%@ Control Language="C#" AutoEventWireup="false" CodeBehind="ImportDocuments.ascx.cs" Inherits="R7.Documents.ImportDocuments" %>
<%@ Register TagPrefix="dnn" TagName="Label" Src="~/controls/LabelControl.ascx" %>
<%@ Register TagPrefix="dnn" Namespace="DotNetNuke.Web.UI.WebControls" Assembly="DotNetNuke.Web" %>

<div class="dnnForm dnnClear">
	<fieldset>
		<div class="dnnFormItem">
			<dnn:Label id="labelModule" runat="server" ControlName="comboModule" Suffix=":" />
			<dnn:DnnComboBox id="comboModule" runat="server" /> 
		</div>
		<ul class="dnnActions dnnClear">
	        <li><asp:LinkButton id="cmdUpdate" runat="server" CssClass="dnnPrimaryAction" resourcekey="cmdUpdate" Text="Update" /></li>
	        <li><asp:HyperLink id="linkCancel" runat="server" CssClass="dnnSecondaryAction" resourcekey="cmdCancel" Text="Cancel" /></li>
	    </ul>
	</fieldset>
</div>
