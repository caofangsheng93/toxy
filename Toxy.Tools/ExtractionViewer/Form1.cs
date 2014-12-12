﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using Toxy;
using unvell.ReoGrid;

namespace ExtractionViewer
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }

        private void exitToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Application.Exit();
        }

        string filepath = null;

        private void openToolStripMenuItem_Click(object sender, EventArgs e)
        {
            OpenFileDialog dialog = new OpenFileDialog();
            dialog.Multiselect = false;
            dialog.Filter = "All Supported Files |*.csv;*.txt;*.xls;*.xlsx;*.docx;*.rtf;*.eml;*.xml;*.html;*.htm;*.pdf;*.vcf";
            dialog.Filter += "|Comma Seperated Files (*.csv)|*.csv";
            dialog.Filter += "|Text Files (*.txt)|*.txt";
            dialog.Filter += "|All Excel Files|*.xls;*.xlsx";
            dialog.Filter += "|Rich Text Files (*.rtf)|*.rtf";
            dialog.Filter += "|Word 2007-2013 Files (*.docx)|*.docx";
            dialog.Filter += "|Business Card Files (*.vcf)|*.vcf";
            dialog.Filter += "|Email Files (*.eml)|*.eml";
            dialog.Filter += "|Html Files (*.html, *.htm)|*.html;*.htm";
            dialog.Filter += "|XML Files (*.xml)|*.xml";
            dialog.Filter += "|Adobe PDF Files (*.pdf)|*.pdf";

            if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                filepath = dialog.FileName;
            }

            OpenFile(filepath, comboBox1.Text);
        }


        //private System.Windows.Forms.RichTextBox richTextBox1;
        RichTextBoxPanel rtbPanel;
        SpreadsheetPanel ssPanel;
        GridPanel gridPanel;
        TreeViewPanel treePanel;

        private void AppendRichTextBox()
        {
            this.splitContainer1.Panel1.Controls.Clear();
            this.rtbPanel = new RichTextBoxPanel();
            this.rtbPanel.Dock = DockStyle.Fill;
            this.splitContainer1.Panel1.Controls.Add(this.rtbPanel);
        }
        private void AppendDataGridView()
        {
            this.splitContainer1.Panel1.Controls.Clear();
            this.gridPanel = new GridPanel();
            this.gridPanel.Dock = DockStyle.Fill;
            this.splitContainer1.Panel1.Controls.Add(this.gridPanel);
        }
        private void AppendSpreadsheetGrid()
        {
            this.splitContainer1.Panel1.Controls.Clear();
            this.ssPanel = new SpreadsheetPanel();
            this.ssPanel.Dock = DockStyle.Fill;
            this.splitContainer1.Panel1.Controls.Add(this.ssPanel);
        }

        private void AppendTreePanel()
        {
            this.splitContainer1.Panel1.Controls.Clear();
            this.treePanel = new TreeViewPanel();
            this.treePanel.Dock = DockStyle.Fill;
            this.splitContainer1.Panel1.Controls.Add(this.treePanel);
        }
        ToxySpreadsheet ss = null;
        
        private void OpenFile(string filepath, string encoding)
        {

            if (string.IsNullOrWhiteSpace(filepath))
            {
                tbPath.Clear();
                return;
            }
            
            tbPath.Text = filepath;
            FileInfo fi = new FileInfo(filepath);
            string extension = fi.Extension.ToLower();
            tbExtension.Text = extension;

            panel1.Visible = false;
            switch (extension)
            {
                case ".pdf":
                case ".docx":
                case ".csv":
                case ".eml":
                case ".vcf":
                case ".html":
                case ".htm":
                case ".xml":
                    textModeToolStripMenuItem.Checked = true;
                    textModeToolStripMenuItem.Enabled = true;
                    documentObjectModeToolStripMenuItem.Checked = false;
                    documentObjectModeToolStripMenuItem.Enabled = true;
                    break;
                case ".txt":
                    textModeToolStripMenuItem.Enabled = true;
                    textModeToolStripMenuItem.Checked = true;
                    documentObjectModeToolStripMenuItem.Enabled = false;
                    documentObjectModeToolStripMenuItem.Checked = false;
                    break;
                case ".rtf":
                case ".xlsx":
                case ".xls":
                    textModeToolStripMenuItem.Enabled = false;
                    textModeToolStripMenuItem.Checked = false;
                    documentObjectModeToolStripMenuItem.Checked = true;
                    documentObjectModeToolStripMenuItem.Enabled = true;
                    break;
                default:
                    AppendRichTextBox();
                    rtbPanel.Text = "Unknown document";
                    tbParserType.Text = "Unknown";
                    return;
            }
            ShowDocument(filepath, encoding, extension);
        }
        private void ShowDocument(string filepath, string encoding, string extension)
        {
            ParserContext context = new ParserContext(filepath);
            context.Encoding = Encoding.GetEncoding(encoding);

            if (Mode == ViewMode.Text)
            {
                AppendRichTextBox();
                var tparser = ParserFactory.CreateText(context);
                rtbPanel.Text = tparser.Parse();
                tbParserType.Text = tparser.GetType().Name;
            }
            else
            {
                switch (extension)
                {
                    case ".csv":
                        AppendSpreadsheetGrid();
                        context.Properties.Add("HasHeader", "1");
                        ISpreadsheetParser csvparser = ParserFactory.CreateSpreadsheet(context);
                        ss = csvparser.Parse();
                        tbParserType.Text = csvparser.GetType().Name;
                        var table1 = ss.Tables[0];
                        ShowToGrid(table1);
                        cbSheets.Items.Clear();
                        foreach (var table in ss.Tables)
                        {
                            cbSheets.Items.Add(table.Name);
                        }
                        cbSheets.SelectedIndex = 0;
                        panel1.Visible = true;
                        break;
                    case ".xlsx":
                    case ".xls":
                        AppendSpreadsheetGrid();
                        ISpreadsheetParser ssparser = ParserFactory.CreateSpreadsheet(context);
                        ss = ssparser.Parse();
                        tbParserType.Text = ssparser.GetType().Name;
                        //DataSet ds = ss.ToDataSet();
                        //dataGridView1.DataSource = ds.Tables[0].DefaultView;
                        var table0 = ss.Tables[0];
                        ShowToGrid(table0);
                        cbSheets.Items.Clear();
                        foreach (var table in ss.Tables)
                        {
                            cbSheets.Items.Add(table.Name);
                        }
                        cbSheets.SelectedIndex = 0;
                        panel1.Visible = true;
                        break;
                    case ".vcf":
                        AppendDataGridView();
                        var vparser = ParserFactory.CreateVCard(context);
                        ToxyBusinessCards vcards = vparser.Parse();
                        tbParserType.Text = vparser.GetType().Name;
                        gridPanel.GridView.DataSource = vcards.ToDataTable().DefaultView;
                        break;
                    case ".xml":
                    case ".htm":
                    case ".html":
                        AppendTreePanel();
                        var domparser = ParserFactory.CreateDom(context);
                        ToxyDom htmlDom = domparser.Parse();
                        TreeNode rootNode = treePanel.Tree.Nodes.Add(htmlDom.Root.NodeString);
                        treePanel.Tree.BeginUpdate();
                        AppendTree(rootNode, htmlDom.Root);
                        treePanel.Tree.EndUpdate();
                        //rootNode.ExpandAll();
                        break;
                }
            }
        }
        void AppendTree(TreeNode node, ToxyNode tnode)
        { 
           if(tnode.ChildrenNodes==null||tnode.ChildrenNodes.Count==0)
               return;
           foreach (var child in tnode.ChildrenNodes)
           {
               TreeNode childNode ;
               if(child.Name=="#text")
                   childNode = node.Nodes.Add(child.Text);
               else
                    childNode = node.Nodes.Add(child.NodeString);
               AppendTree(childNode, child);
           }
        }
        private void ShowToGrid(ToxyTable table)
        {
            ssPanel.ReoGridControl.Reset();
            ssPanel.ReoGridControl.ColCount = table.LastColumnIndex + 1;
            ssPanel.ReoGridControl.RowCount = table.LastRowIndex + 2; 
            if(table.HasHeader)
                foreach (var cell in table.ColumnHeaders.Cells)
                {
                    ssPanel.ReoGridControl.SetCellData(new ReoGridPos(0, cell.CellIndex), cell.Value);
                }
            foreach (var row in table.Rows)
            {
                foreach (var cell in row.Cells)
                {
                    ssPanel.ReoGridControl.SetCellData(new ReoGridPos(row.RowIndex+1, cell.CellIndex), cell.Value);
                }
            }
            foreach (var cellrange in table.MergeCells)
            {
                ssPanel.ReoGridControl.MergeRange(new ReoGridRange(
                    new ReoGridPos(cellrange.FirstRow, cellrange.FirstColumn),
                    new ReoGridPos(cellrange.LastRow, cellrange.LastColumn)));
            }
        }
        private void btnReopen_Click(object sender, EventArgs e)
        {
            OpenFile(filepath, comboBox1.Text);
        }

        private void btnSelectSheet_Click(object sender, EventArgs e)
        {
            if (ss == null)
            {
                return;
            }
            var table= ss[cbSheets.Text];
            if(table==null)
                return;
            ShowToGrid(table);
           
        }

        private void aboutToolStripMenuItem1_Click(object sender, EventArgs e)
        {
            AboutBox1 ab = new AboutBox1();
            ab.ShowDialog();
        }
        public enum ViewMode
        { 
            Text,
            Structured
        }
        ViewMode Mode { 
            get {
                if (textModeToolStripMenuItem.Checked)
                    return ViewMode.Text;
                else
                    return ViewMode.Structured;
            } 
        }
        void SwitchMode(ViewMode mode)
        {
            if (mode == ViewMode.Text)
            {
                documentObjectModeToolStripMenuItem.Checked = false;
                textModeToolStripMenuItem.Checked = true;
            }
            else
            {
                documentObjectModeToolStripMenuItem.Checked = true;
                textModeToolStripMenuItem.Checked = false;          
            }
        }

        private void documentObjectModeToolStripMenuItem_Click(object sender, EventArgs e)
        {
            SwitchMode(ViewMode.Structured);
            ShowDocument(filepath, comboBox1.Text, tbExtension.Text);
        }

        private void textModeToolStripMenuItem_Click(object sender, EventArgs e)
        {
            SwitchMode(ViewMode.Text);
            ShowDocument(filepath, comboBox1.Text, tbExtension.Text);
        }

    }
}
