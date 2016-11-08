using System;
using System.Collections.Generic;
using System.IO;
using System.Windows.Forms;

namespace WebMConverter
{
    public partial class SubtitleForm : Form
    {
        public SubtitleFilter GeneratedFilter;

        public SubtitleForm()
        {
            InitializeComponent();

            if (Program.InputHasWeirdPixelFormat)
            {
                MessageBox.Show(
                    "Your input video is Hi444p! That's cool, but the subtitle renderer can't deal with that pixel format.\nIf you add subtitles to your webm, we'll have to convert it to something a bit less cool.\nIt's handed automatically, though. Don't worry.",
                    "FYI", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }

            if (Program.SubtitleTracks.Count == 0)
            {
                checkBoxInternalSubs.Checked = false;
                checkBoxInternalSubs.Enabled = false;
            }
            else
            {
                Dictionary<int, string> subtitleTracks = new Dictionary<int, string>();
                foreach (var track in Program.SubtitleTracks)
                {
                    subtitleTracks.Add(track.Key, string.Format("#{0}: {1}", track.Key, track.Value.Item1));
                }
                comboBoxVideoTracks.DataSource = new BindingSource(subtitleTracks, null);
                comboBoxVideoTracks.ValueMember = "Key";
                comboBoxVideoTracks.DisplayMember = "Value";
            }
        }

        public SubtitleForm(SubtitleFilter SubtitleFilter) : this()
        {
            if (SubtitleFilter.FileName == Program.InputFile)
            {
                comboBoxVideoTracks.SelectedValue = SubtitleFilter.Track;
            }
            else
            {
                checkBoxInternalSubs.Checked = false;
                textBoxSubtitleFile.Text = SubtitleFilter.FileName;
                if (Program.SubtitleTracks.Count == 0)
                    checkBoxInternalSubs.Enabled = false;
            }
        }

        private void buttonConfirm_Click(object sender, EventArgs e)
        {
            if (checkBoxInternalSubs.Checked)
            {
                SubtitleType type = Program.SubtitleTracks[(int)comboBoxVideoTracks.SelectedValue].Item2;
                string extension;
                switch (type)
                {
                    case SubtitleType.TextSub:
                        extension = Program.SubtitleTracks[(int)comboBoxVideoTracks.SelectedValue].Item3;
                        break;
                    case SubtitleType.VobSub:
                        extension = ".idx";
                        break;
                    default:
                        throw new NotImplementedException();
                }
                string filename = Path.Combine(Program.AttachmentDirectory, string.Format("sub{0}{1}", (int)comboBoxVideoTracks.SelectedValue, extension));
                GeneratedFilter = new SubtitleFilter(filename, type, (int)comboBoxVideoTracks.SelectedValue);
            }
            else
            {
                string filename = textBoxSubtitleFile.Text;
                SubtitleType type = Path.GetExtension(filename) == ".sub" ? SubtitleType.VobSub : SubtitleType.TextSub;
                GeneratedFilter = new SubtitleFilter(filename, type);
            }
        }

        private void checkBoxInternalSubs_CheckedChanged(object sender, EventArgs e)
        {
            tableLayoutPanelSubtitleFileSelector.Visible = !checkBoxInternalSubs.Checked;
            comboBoxVideoTracks.Visible = checkBoxInternalSubs.Checked;
            label2.Text = checkBoxInternalSubs.Checked ? "Subtitle track:" : "Subtitle file:";
            checkBoxInternalSubs.Text = checkBoxInternalSubs.Checked ? "Yes" : "No";
        }

        private void buttonSelectSubtitleFile_Click(object sender, EventArgs e)
        {
            using (var dialog = new OpenFileDialog())
            {
                dialog.InitialDirectory = Path.GetDirectoryName(Program.InputFile);
                dialog.Filter = "Text subtitles (*.ass, *.srt, *.ssa)|*.ass;*.srt;*.ssa|DVD subtitles (*.sub)|*.sub";
                dialog.RestoreDirectory = true;

                if (dialog.ShowDialog() == DialogResult.OK)
                {
                    textBoxSubtitleFile.Text = dialog.FileName;
                }
            }
        }
    }

    public class SubtitleFilter
    {
        public readonly string FileName;
        public readonly SubtitleType Type;
        public readonly int Track;

        public SubtitleFilter(string fileName, SubtitleType type, int track = -1)
        {
            FileName = fileName;
            Type = type;
            Track = track;
        }

        public override string ToString()
        {
            var conversion = Program.InputHasWeirdPixelFormat ? "ConvertToYV12() " : "";

            switch (Type)
            {
                case SubtitleType.TextSub:
                    return string.Format(@"{1}textsub(""{0}"")", FileName, conversion);
                case SubtitleType.VobSub:
                    return string.Format(@"{1}vobsub(""{0}"")", FileName, conversion);
                default:
                    throw new NotImplementedException();
            }
        }
    }
}
