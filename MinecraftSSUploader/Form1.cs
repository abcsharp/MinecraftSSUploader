﻿using System;
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

namespace MinecraftSSUploader
{
	public partial class Form1 : Form
	{
		Setting CurrentSetting;
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
			CurrentSetting=File.Exists(SettingFileName)?new Setting(SettingFileName):new Setting();
			ScreenShotDirectory=new DirectoryInfo(Environment.ExpandEnvironmentVariables("%APPDATA%\\.minecraft\\screenshots"));
			InitializeComponent();
			return;
		}

		private void Form1_Load(object sender,EventArgs e)
		{
			Location=CurrentSetting.WindowPosition;
			Size=CurrentSetting.WindowSize;
			WindowState=CurrentSetting.IsMaximized?FormWindowState.Maximized:FormWindowState.Normal;
			textBox1.Text=CurrentSetting.UploaderPath;
			checkBox1.Checked=CurrentSetting.BootLauncher;
			fileSystemWatcher1.Path=ScreenShotDirectory.FullName;
			string LauncherPath=Environment.CurrentDirectory+"\\Minecraft.exe";
			if(checkBox1.Checked&&File.Exists(LauncherPath)){
				var WindowHandle=Interop.FindWindow(null,"Minecraft Launcher");
				if(WindowHandle==IntPtr.Zero) Process.Start(LauncherPath);
			}
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
			CurrentSetting.IsMaximized=WindowState==FormWindowState.Maximized;
			if(!CurrentSetting.IsMaximized){
				CurrentSetting.WindowSize=Size;
				CurrentSetting.WindowPosition=Location;
			}
			CurrentSetting.UploaderPath=textBox1.Text;
			CurrentSetting.BootLauncher=checkBox1.Checked;
			CurrentSetting.Save(SettingFileName);
			return;
		}

		private void fileSystemWatcher1_Created(object sender,FileSystemEventArgs e)
		{
			UpdateListItems();
			return;
		}

		private void fileSystemWatcher1_Renamed(object sender,RenamedEventArgs e)
		{
			UpdateListItems();
			return;
		}

		private void listBox1_SelectedIndexChanged(object sender,EventArgs e)
		{
			timer1.Enabled=true;
			return;
		}

		private void UpdateListItems()
		{
			listBox1.BeginUpdate();
			var Files=ScreenShotDirectory.GetFiles("*.png",SearchOption.TopDirectoryOnly).ToList();
			Files.Sort((a,b)=>a.CreationTime.Ticks.CompareTo(b.CreationTime.Ticks));
			var Items=from Item in Files select new FileItem(Item.Name,Item.FullName);
			listBox1.DataSource=Items.ToList();
			listBox1.DisplayMember="ShortName";
			listBox1.ValueMember="FullName";
			listBox1.EndUpdate();
			listBox1.SelectedIndex=listBox1.Items.Count-1;
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
			foreach(FileInfo File in ScreenShotDirectory.GetFiles()) File.Delete();
			UpdateListItems();
			pictureBox1.Image=null;
		}

	}

	public class Setting
	{
		public Setting()
		{
			Init1();
			Init2();
			return;
		}

		public Setting(string FileName)
		{
			var Parser=new JsonParser();
			var Result=(Dictionary<string,object>)Parser.Parse(File.ReadAllText(FileName,Encoding.UTF8));
			WindowPosition=new Point((int)(long)Result["WindowPositionX"],(int)(long)Result["WindowPositionY"]);
			WindowSize=new Size((int)(long)Result["WindowWidth"],(int)(long)Result["WindowHeight"]);
			IsMaximized=(bool)Result["IsMaximized"];
			UploaderPath=(string)Result["UploaderPath"];
			if(!Result.ContainsKey("BootLauncher")) Init2();
			else BootLauncher=(bool)Result["BootLauncher"];
			return;
		}

		private void Init1()
		{
			WindowPosition=new Point(100,100);
			WindowSize=new Size(750,370);
			IsMaximized=false;
			UploaderPath="";
			return;
		}

		private void Init2()
		{
			BootLauncher=false;
			return;
		}

		public void Save(string FileName)
		{
			var Creator=new JsonCreator();
			var Dict=new Dictionary<string,object>();
			Dict.Add("WindowPositionX",WindowPosition.X);
			Dict.Add("WindowPositionY",WindowPosition.Y);
			Dict.Add("WindowWidth",WindowSize.Width);
			Dict.Add("WindowHeight",WindowSize.Height);
			Dict.Add("IsMaximized",IsMaximized);
			Dict.Add("UploaderPath",UploaderPath);
			Dict.Add("BootLauncher",BootLauncher);
			File.WriteAllText(FileName,Creator.Create(Dict),Encoding.UTF8);
			return;
		}

		public Point WindowPosition{get;set;}
		public Size WindowSize{get;set;}
		public bool IsMaximized{get;set;}
		public string UploaderPath{get;set;}
		public bool BootLauncher{get;set;}
	}

	public class Interop
	{
		[DllImport("user32.dll",CharSet=CharSet.Unicode,EntryPoint="FindWindowW")]
		public static extern IntPtr FindWindow(string ClassName,string WindowCaption);
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