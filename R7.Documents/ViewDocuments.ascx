﻿<%@ Control Language="C#" AutoEventWireup="false" CodeBehind="ViewDocuments.ascx.cs" Inherits="R7.Documents.ViewDocuments" %>
<div class="ViewDocuments">
  <asp:GridView id="grdDocuments" runat="server" DataKeyField="ItemID" EnableViewState="false" AutoGenerateColumns="false" GridLines="None" Width="100%"
        OnSorting="grdDocuments_Sorting" OnRowCreated="grdDocuments_RowCreated">
    <Columns>
      <asp:TemplateField>
        <ItemTemplate>
          <asp:HyperLink id="linkEdit" runat="server" Visible="<%# IsEditable %>" 
				NavigateUrl='<%# EditUrl("ItemID",DataBinder.Eval(Container.DataItem,"ItemID").ToString()) %>' >
            <asp:Image id="imageEdit" runat="server" ImageUrl="<%# EditImageUrl %>" 
				ToolTip='<%# DataBinder.Eval(Container.DataItem,"Info").ToString() %>' />
          </asp:HyperLink>
      	</ItemTemplate>
      </asp:TemplateField>
    </Columns>
  </asp:GridView>
</div>
