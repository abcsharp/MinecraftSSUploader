using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Xml;
using System.IO;
using System.Text.RegularExpressions;
using System.Diagnostics;
using System.Runtime.InteropServices;
using Json;
using MinecraftSSUploader.Properties;

namespace MinecraftSSUploader
{
	public partial class Form1 : Form
	{
		DirectoryInfo ScreenShotDirectory;
		string SettingFileName
		{
			get
			{
				return "MCSSUPSetting.json";
			}
		}

		public Form1()
		{
			ScreenShotDirectory=new DirectoryInfo(Environment.ExpandEnvironmentVariables("%APPDATA%\\.minecraft\\screenshots"));
			if(!ScreenShotDirectory.Exists){
				MessageBox.Show("screenshotsディレクトリが存在しません。",
					"Minecraft Screen Shot Uploader",
					MessageBoxButtons.OK,
					MessageBoxIcon.Error);
				Application.Exit();
			}
			InitializeComponent();
			fileSystemWatcher1.Path=ScreenShotDirectory.FullName;
			fileSystemWatcher1.InternalBufferSize=32768;
			return;
		}

		private void Form1_Load(object sender,EventArgs e)
		{
			Size=Settings.Default.WindowSize;
			WindowState=Settings.Default.IsMaximized?FormWindowState.Maximized:FormWindowState.Normal;
			textBox1.Text=Settings.Default.UploaderPath;
			UpdateListItems();
			return;
		}

		private void label2_DragEnter(object sender,DragEventArgs e)
		{
			if(e.Data.GetDataPresent(DataFormats.FileDrop)) e.Effect=DragDropEffects.All;
			else e.Effect=DragDropEffects.None;
			return;
		}

		private void label2_DragDrop(object sender,DragEventArgs e)
		{
			string FileName=((string[])e.Data.GetData(DataFormats.FileDrop))[0];
			if(!File.Exists(FileName)||new FileInfo(FileName).Extension.ToLower()!=".exe"){
				MessageBox.Show("ドロップされたファイルはディレクトリか\nアプリケーションではないファイルです。",
					"Minecraft Screen Shot Uploader",
					MessageBoxButtons.OK,
					MessageBoxIcon.Error);
				return;
			}
			textBox1.Text=FileName;
			return;
		}

		private void Form1_FormClosing(object sender,FormClosingEventArgs e)
		{
			Settings.Default.IsMaximized=WindowState==FormWindowState.Maximized;
			if(!Settings.Default.IsMaximized) Settings.Default.WindowSize=Size;
			Settings.Default.UploaderPath=textBox1.Text;
			Settings.Default.Save();
			return;
		}

		private void fileSystemWatcher1_Created(object sender,FileSystemEventArgs e)
		{
			var fileInfo=new FileInfo(e.FullPath);
			if(fileInfo.Extension.ToLower()==".png"){
				var item=new FileItem(fileInfo.Name,fileInfo.FullName);
				bindingSource1.Add(item);
				bindingSource1.Position=bindingSource1.Count-1;
			}
			return;
		}

		private void fileSystemWatcher1_Renamed(object sender,RenamedEventArgs e)
		{
			var fileInfo=new FileInfo(e.FullPath);
			if(fileInfo.Extension.ToLower()==".png"){
				var item=bindingSource1.List.Cast<FileItem>().FirstOrDefault(i=>i.FullName.Equals(e.OldFullPath,StringComparison.OrdinalIgnoreCase));
				if(item!=null){
					item.FullName=fileInfo.FullName;
					item.ShortName=fileInfo.Name;
					bindingSource1.ResetItem(bindingSource1.List.IndexOf(item));
				}
			}
			return;
		}

		private void fileSystemWatcher1_Deleted(object sender,FileSystemEventArgs e)
		{
			var fileInfo=new FileInfo(e.FullPath);
			if(fileInfo.Extension.ToLower()==".png"){
				var item=bindingSource1.List.Cast<FileItem>().FirstOrDefault(i=>i.FullName.Equals(fileInfo.FullName,StringComparison.OrdinalIgnoreCase));
				if(item!=null) bindingSource1.Remove(item);
			}
		}

		private void listBox1_SelectedIndexChanged(object sender,EventArgs e)
		{
			timer1.Enabled=false;
			timer1.Enabled=true;
			return;
		}

		private void UpdateListItems()
		{
			listBox1.BeginUpdate();
			var Files=ScreenShotDirectory.GetFiles("*.png",SearchOption.TopDirectoryOnly).ToList();
			Files.Sort((a,b)=>a.LastWriteTime.Ticks.CompareTo(b.LastWriteTime.Ticks));
			var Items=from Item in Files select new FileItem(Item.Name,Item.FullName);
			bindingSource1.DataSource=Items.ToList();
			bindingSource1.Position=bindingSource1.Count-1;
			listBox1.EndUpdate();
			return;
		}

		private void button1_Click(object sender,EventArgs e)
		{
			Process.Start(textBox1.Text,(string)listBox1.SelectedValue);
			return;
		}

		private void button2_Click(object sender,EventArgs e)
		{
			if(openFileDialog1.ShowDialog()==DialogResult.OK) textBox1.Text=openFileDialog1.FileName;
			return;
		}

		private void timer1_Tick(object sender,EventArgs e)
		{
			if(listBox1.SelectedValue.GetType()==typeof(string)){
				pictureBox1.LoadAsync((string)listBox1.SelectedValue);
			}
			timer1.Enabled=false;
			return;
		}

		private void pictureBox1_LoadCompleted(object sender,AsyncCompletedEventArgs e)
		{
			if(e.Error!=null) timer1.Enabled=true;
		}

		private void button3_Click(object sender,EventArgs e)
		{
			var result=MessageBox.Show("画像をすべて削除します。よろしいですか?",
				"Minecraft Screen Shot Uploader",
				MessageBoxButtons.YesNo,
				MessageBoxIcon.Warning);
			if(result==DialogResult.Yes){
				bindingSource1.Clear();
				foreach(FileInfo File in ScreenShotDirectory.GetFiles()) File.Delete();
				pictureBox1.Image=null;
			}
		}

		private void button4_Click(object sender,EventArgs e)
		{
			if(listBox1.SelectedItem!=null){
				var result=MessageBox.Show("この画像を削除します。よろしいですか?",
					"Minecraft Screen Shot Uploader",
					MessageBoxButtons.YesNo,
					MessageBoxIcon.Warning);
				if(result==DialogResult.Yes){
					var item=(FileItem)listBox1.SelectedItem;
					bindingSource1.Remove(item);
					File.Delete(item.FullName);
					timer1.Enabled=true;
				}
			}
		}
	}

	public class FileItem
	{
		public FileItem(string Short,string Full)
		{
			ShortName=Short;
			FullName=Full;
			return;
		}

		public string ShortName{get;set;}
		public string FullName{get;set;}
	}
}
