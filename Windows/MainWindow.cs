﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using VocalUtau.Formats.Model.USTs.Original;
using VocalUtau.Formats.Model.VocalObject;
using VocalUtau.DirectUI.Forms;
using VocalUtau.ActionWorker;
using VocalUtau.ObjectUtils;
using VocalUtau.DirectUI.Utils.PianoUtils;
using System.Threading;

namespace VocalUtau.Windows
{
    public partial class MainWindow : Form
    {
        SingerWindow sw = new SingerWindow();
        AttributesWindow aw = new AttributesWindow();
        TrackerWindow tw = new TrackerWindow();
        UndoAbility UndoAbility;
        MainFormWorker Controller;
        public MainWindow()
        {
            InitializeComponent();
            sw.ShowOnDock(this.MainDock);
            aw.ShowOnDock(this.MainDock);
            tw.ShowOnDock(this.MainDock);
            
            UndoAbility = new UndoAbility(ref sw, ref aw, ref tw);
            UndoAbility.UndoWorked += UndoAbility_UndoWorked;
            Controller = new MainFormWorker(ref sw, ref aw, ref tw, ref UndoAbility);

            ToolStrip_OpenAble.Location = new Point(1, 0);

            sw.BaseController.ToolStatusChange += BaseController_ToolStatusChange;
            sw.NoteCopyMemoryChanged += sw_NoteCopyMemoryChanged;
            sw.NoteSelectListChange += sw_NoteSelectListChange;

            tw.TotalTimePosChange += TotalTimePosChange;
            sw.TotalTimePosChange += TotalTimePosChange;
            this.ShowInTaskbar = false;
        }

        void TotalTimePosChange(double Time)
        {
            TimeSpan Ts = new TimeSpan(0, 0, 0, 0, (int)(Time*1000));
            //toolBtn_Mixer.Text = String.Format("{0:00}:{1:00}.{2:000}", Ts.TotalMinutes, Ts.Seconds, Math.Round(Ts.Milliseconds>0?Ts.TotalMilliseconds:0,3));
            aw.CurrentPosTime(Ts);
        }

        void sw_NoteSelectListChange(List<int> SelectedIndexs)
        {
            BindNoteCopys();
        }

        void sw_NoteCopyMemoryChanged(bool isCopyed)
        {
            BindNoteCopys();
        }

        void BaseController_ToolStatusChange(object StatusEnum)
        {
            this.toolBtn_NoteSelect.Checked=false;
            this.toolBtn_NoteAdd.Checked=false;
            this.toolBtn_G_Line.Checked=false;
            this.toolBtn_G_J.Checked=false;
            this.toolBtn_G_R.Checked=false;
            this.toolBtn_G_S.Checked=false;
            this.toolBtn_G_Earse.Checked=false;
            if (StatusEnum.GetType() == typeof(VocalUtau.DirectUI.Utils.PianoUtils.NoteView.NoteToolsType))
            {
                VocalUtau.DirectUI.Utils.PianoUtils.NoteView.NoteToolsType sw = (VocalUtau.DirectUI.Utils.PianoUtils.NoteView.NoteToolsType)StatusEnum;
                switch (sw)
                {
                    case VocalUtau.DirectUI.Utils.PianoUtils.NoteView.NoteToolsType.Select: this.toolBtn_NoteSelect.Checked = true; break;
                    case VocalUtau.DirectUI.Utils.PianoUtils.NoteView.NoteToolsType.Add: this.toolBtn_NoteAdd.Checked = true; break;
                }
            }
            else if (StatusEnum.GetType() == typeof(VocalUtau.DirectUI.Utils.PianoUtils.PitchView.PitchDragingType))
            {
                VocalUtau.DirectUI.Utils.PianoUtils.PitchView.PitchDragingType sw = (VocalUtau.DirectUI.Utils.PianoUtils.PitchView.PitchDragingType)StatusEnum;
                switch (sw)
                {
                    case VocalUtau.DirectUI.Utils.PianoUtils.PitchView.PitchDragingType.DrawLine: this.toolBtn_G_Line.Checked = true; break;
                    case VocalUtau.DirectUI.Utils.PianoUtils.PitchView.PitchDragingType.DrawGraphJ: this.toolBtn_G_J.Checked = true; break;
                    case VocalUtau.DirectUI.Utils.PianoUtils.PitchView.PitchDragingType.DrawGraphR: this.toolBtn_G_R.Checked = true; break;
                    case VocalUtau.DirectUI.Utils.PianoUtils.PitchView.PitchDragingType.DrawGraphS: this.toolBtn_G_S.Checked = true; break;
                    case VocalUtau.DirectUI.Utils.PianoUtils.PitchView.PitchDragingType.EarseArea: this.toolBtn_G_Earse.Checked = true; break;
                }
            }
        }

        void UndoAbility_UndoWorked(UndoAbility sender)
        {
            toolBtn_Undo.Enabled = false;
            menuItem_Undo.Enabled = false;
            toolBtn_Repeat.Enabled = false;
            menuItem_Repeat.Enabled = false;
            toolBtn_Undo.Enabled = sender.UndoCount > 0;
            menuItem_Undo.Enabled = sender.UndoCount > 0;
            toolBtn_Undo.AutoToolTip = false;
            toolBtn_Undo.ToolTipText = sender.UndoCount>0?"撤销-"+sender.LastUndoRepo:"撤销";
            menuItem_Undo.Text = toolBtn_Undo.ToolTipText +"(&U)";

            toolBtn_Repeat.Enabled = sender.RepeatCount > 0;
            menuItem_Repeat.Enabled = sender.RepeatCount > 0;
            toolBtn_Repeat.AutoToolTip = false;
            toolBtn_Repeat.ToolTipText = sender.RepeatCount > 0 ? "重复-" + sender.LastRepeatRepo : "重复";
            menuItem_Repeat.Text = toolBtn_Repeat.ToolTipText + "(&R)";
        }
        private void MainWindow_Load(object sender, EventArgs e)
        {
            this.Visible = false;
            Controller.NewProject();
            
            SplashForm sf = new SplashForm();
            sf.TopMost = true;
            sf.Show();
            Thread SplashWork = new Thread(new ParameterizedThreadStart((work) => {
                MainWindow MWin = (MainWindow)((object[])work)[0];
                SplashForm SWin = (SplashForm)((object[])work)[1];
                if (Program.GlobalPackage.InitGlobal(SWin))
                {
                    SWin.Invoke(new Action(()=>{SWin.Close();}));
                    MWin.Invoke(new Action(() =>
                    {
                        this.WindowState = FormWindowState.Maximized;
                        this.ToolStrip_OpenAble.Location = new Point(1, 0);
                        this.Visible = true;
                        this.ShowInTaskbar = true;
                        Controller.NewProject();
                    }));
                }
                else
                {
                    this.Close();
                    Application.Exit();
                }
            }));
            SplashWork.Start(new object[] { this, sf });
        }

        private void toolBtn_Undo_Click(object sender, EventArgs e)
        {
            UndoAbility.RegisterPoint();
            string Undo = UndoAbility.LastUndoRepo;
            UndoAbility.ApplyUndoPoint();
            UndoAbility.AddRepeatPoint(Undo);
        }

        private void toolBtn_Repeat_Click(object sender, EventArgs e)
        {
            UndoAbility.RegisterPoint();
            string Repeat = UndoAbility.LastRepeatRepo;
            UndoAbility.ApplyRepeatPoint();
            UndoAbility.AddUndoPoint(Repeat);
        }

        private void toolBtn_NoteSelect_Click(object sender, EventArgs e)
        {
            sw.BaseController.SetNoteViewTool(NoteView.NoteToolsType.Select);
            if (!sw.BaseController.BindRollAndParam)
            {
                sw.BaseController.SetParamGraphicTool(PitchView.PitchDragingType.DrawGraphS);
            }
        }

        private void toolBtn_NoteAdd_Click(object sender, EventArgs e)
        {
            sw.BaseController.SetNoteViewTool(NoteView.NoteToolsType.Add);
            if (!sw.BaseController.BindRollAndParam)
            {
                sw.BaseController.SetParamGraphicTool(PitchView.PitchDragingType.DrawGraphS);
            }
        }

        private void toolBtn_G_Line_Click(object sender, EventArgs e)
        {
            sw.BaseController.SetPitchViewTool(PitchView.PitchDragingType.DrawLine);
            if (!sw.BaseController.BindRollAndParam)
            {
                sw.BaseController.SetParamGraphicTool(PitchView.PitchDragingType.DrawLine);
            }
        }

        private void toolBtn_G_J_Click(object sender, EventArgs e)
        {
            sw.BaseController.SetPitchViewTool(PitchView.PitchDragingType.DrawGraphJ);
            if (!sw.BaseController.BindRollAndParam)
            {
                sw.BaseController.SetParamGraphicTool(PitchView.PitchDragingType.DrawGraphJ);
            }
        }

        private void toolBtn_G_R_Click(object sender, EventArgs e)
        {
            sw.BaseController.SetPitchViewTool(PitchView.PitchDragingType.DrawGraphR);
            if (!sw.BaseController.BindRollAndParam)
            {
                sw.BaseController.SetParamGraphicTool(PitchView.PitchDragingType.DrawGraphR);
            }
        }

        private void toolBtn_G_S_Click(object sender, EventArgs e)
        {
            sw.BaseController.SetPitchViewTool(PitchView.PitchDragingType.DrawGraphS);
            if (!sw.BaseController.BindRollAndParam)
            {
                sw.BaseController.SetParamGraphicTool(PitchView.PitchDragingType.DrawGraphS);
            }
        }

        private void toolBtn_G_Earse_Click(object sender, EventArgs e)
        {
            sw.BaseController.SetPitchViewTool(PitchView.PitchDragingType.EarseArea);
            if (!sw.BaseController.BindRollAndParam)
            {
                sw.BaseController.SetParamGraphicTool(PitchView.PitchDragingType.EarseArea);
            }
        }
        
        void BindNoteCopys()
        {
            this.toolBtn_NotePaste.Enabled = sw.BaseController.CopyPasteController.isCopyed;
            this.editItem_PasteNotes.Enabled = this.toolBtn_NotePaste.Enabled;
            this.toolBtn_NoteCopy.Enabled = sw.BaseController.Track_NoteView.SelectedCount > 0;
            this.editItem_CopyNotes.Enabled = this.toolBtn_NoteCopy.Enabled;
            this.toolBtn_NoteDelete.Enabled = sw.BaseController.Track_NoteView.SelectedCount > 0;
            this.editItem_DeleteNotes.Enabled = this.toolBtn_NoteDelete.Enabled;
            this.toolBtn_LyricEdits.Enabled = sw.BaseController.Track_NoteView.SelectedCount > 0;
            this.editItem_EditLyrics.Enabled = this.toolBtn_LyricEdits.Enabled;
        }

        private void toolBtn_LyricEdits_Click(object sender, EventArgs e)
        {
            sw.EditLyrics();
        }

        private void toolBtn_NoteCopy_Click(object sender, EventArgs e)
        {
            sw.CopyNotes();
        }

        private void toolBtn_NotePaste_Click(object sender, EventArgs e)
        {
            sw.PasteNotes();
        }

        private void toolBtn_NoteDelete_Click(object sender, EventArgs e)
        {
            sw.NoteDeletes();
        }

        private void toolBtn_Open_Click(object sender, EventArgs e)
        {
            if (Controller.OpenProject(this))
            {
                UndoAbility.ClearRepeat();
                UndoAbility.ClearUndo();
                GC.Collect();
            }
        }

        private void toolBtn_New_Click(object sender, EventArgs e)
        {
            Controller.NewProject();
            UndoAbility.ClearRepeat();
            UndoAbility.ClearUndo();
            GC.Collect();
        }

        private void toolBtn_Save_Click(object sender, EventArgs e)
        {
            if (Controller.SaveProject(this))
            {
                ;
            }
        }

        private void FileMenu_Close_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        private void FileMenu_SaveAs_Click(object sender, EventArgs e)
        {

            if (Controller.SaveProject(this,true))
            {
                ;
            }
        }

        private void FileMenu_OpenUSTs_Click(object sender, EventArgs e)
        {
            if (Controller.OpenUSTs(this))
            {
                UndoAbility.ClearRepeat();
                UndoAbility.ClearUndo();
                GC.Collect();
            }
        }

        private void menuItem_EditProjectInformation_Click(object sender, EventArgs e)
        {
            Controller.SetupPassword(this);
        }

        private void menuItem_AboutProgram_Click(object sender, EventArgs e)
        {
            SplashForm sp = new SplashForm(true);
            StringBuilder msgbuilder = new StringBuilder();
            msgbuilder.AppendLine("Chorista歌声合成系统,版本：Violet");
            msgbuilder.AppendLine("Powered By Mitsuteru Hoshino");
            msgbuilder.AppendLine("Alpha Version");
            msgbuilder.AppendLine(Program.GlobalPackage.Configures.GlobalSingerList.Count.ToString() + "SystemSingers Loaded");
            sp.SetupStepMessage(msgbuilder.ToString());
            sp.ShowDialog(this);
        }

        private void toolBtn_Play_Click(object sender, EventArgs e)
        {
            Controller.PlayingWorker.Play(tw.getCurrentTimePos());
        }

        private void trackToolStripMenuItem_DropDownOpening(object sender, EventArgs e)
        {
            VocalUtau.DirectUI.Utils.TrackerUtils.TrackerView.PartLocation pl = tw.BaseController.Track_View.getSelectingParts();
            if (pl == null)
            {
                track_DelectParts.Enabled = false;
                track_DelTracks.Enabled = false;
                track_AddParts.Enabled = false;
                track_ImportAsPart.Enabled = false;
            }
            else
            {
                track_DelectParts.Enabled = true;
                track_DelTracks.Enabled = true;
                track_AddParts.Enabled = true;
                track_ImportAsPart.Enabled = pl.TrackLocation.Type == VocalUtau.DirectUI.Utils.TrackerUtils.TrackerView.TrackLocation.TrackType.Barker;
            }
        }

        private void track_AddNewBackerTrack_Click(object sender, EventArgs e)
        {
            tw.BaseController.AddNewTrack(true);
        }

        private void track_AddNewTrack_Click(object sender, EventArgs e)
        {
            tw.BaseController.AddNewTrack(false);
        }

        private void track_AddParts_Click(object sender, EventArgs e)
        {
            tw.BaseController.AddNewPart();
        }

        private void track_DelTracks_Click(object sender, EventArgs e)
        {
            tw.BaseController.DeleteTrack();
        }

        private void track_DelectParts_Click(object sender, EventArgs e)
        {
            tw.BaseController.DeletePart();
        }

        private void track_ImportAsTrack_Click(object sender, EventArgs e)
        {
            OpenFileDialog ofd = new OpenFileDialog();
            ofd.Filter = "Wav音频文件|*.wav|所有文件|*.*";
            ofd.CheckFileExists = true;
            if (ofd.ShowDialog() == DialogResult.OK)
            {
                tw.BaseController.ImportAsTrack(ofd.FileName);
            }
        }

        private void track_ImportAsPart_Click(object sender, EventArgs e)
        {
            OpenFileDialog ofd = new OpenFileDialog();
            ofd.Filter = "Wav音频文件|*.wav|所有文件|*.*";
            ofd.CheckFileExists = true;
            if (ofd.ShowDialog() == DialogResult.OK)
            {
                tw.BaseController.ImportAsPart(ofd.FileName);
            }
        }

        private void item_TrackNormalize_Click(object sender, EventArgs e)
        {
            try
            {
                tw.BaseController.NormalizeTracksAndParts();
            }
            catch { ;}
        }

        private void item_TrackNormalizeOnly_Click(object sender, EventArgs e)
        {
            try
            {
                tw.BaseController.NormalizeTracks();
            }
            catch { ;}
        }

        private void toolBtn_Stop_Click(object sender, EventArgs e)
        {
            Controller.PlayingWorker.Stop();
        }

        private void MainWindow_FormClosing(object sender, FormClosingEventArgs e)
        {
            Controller.PlayingWorker.Stop();
        }

        private void toolBtn_Paste_Click(object sender, EventArgs e)
        {
            Controller.PlayingWorker.Pause();
        }
        

    }
}
