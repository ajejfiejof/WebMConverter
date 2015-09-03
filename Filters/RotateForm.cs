
using System;
using System.Windows.Forms;
using WebMConverter.Dialogs;

namespace WebMConverter
{
    public partial class RotateForm : Form
    {
        public RotateFilter GeneratedFilter;

        public RotateForm(RotateFilter filterToEdit = null)
        {
            InitializeComponent();

            if (filterToEdit == null) return;

            switch (filterToEdit.Mode)
            {
                case RotateFilter.RotateMode.Right:
                    checkTurnRight.Checked = true;
                    break;
                case RotateFilter.RotateMode.Twice:
                    checkTurnTwice.Checked = true;
                    break;
                case RotateFilter.RotateMode.Left:
                    checkTurnLeft.Checked = true;
                    break;
            }
            checkFlipHorizontal.Checked = filterToEdit.FlipHorizontal;
            checkFlipVertical.Checked = filterToEdit.FlipVertical;
        }

        private void RotateForm_Load(object sender, EventArgs e)
        {
            if ((Owner as MainForm).boxAdvancedScripting.Checked) return;

            if (Filters.Trim != null)
            {
                previewFrame.Frame = Filters.Trim.TrimStart;
                trimTimingToolStripMenuItem.Enabled = true;
            }
            if (Filters.MultipleTrim != null)
            {
                previewFrame.Frame = Filters.MultipleTrim.Trims[0].TrimStart;
            }
        }

        private void frameToolStripMenuItem_Click(object sender, EventArgs e)
        {
            using (var dialog = new InputDialog<int>("Frame", previewFrame.Frame))
            {
                if (dialog.ShowDialog() == DialogResult.OK)
                {
                    previewFrame.Frame = Math.Max(0, Math.Min(Program.VideoSource.NumberOfFrames - 1, dialog.Value)); // Make sure we don't go out of bounds.
                }
            }
        }

        private void timeToolStripMenuItem_Click(object sender, EventArgs e)
        {
            using (var dialog = new InputDialog<TimeSpan>("Time", Program.FrameToTimeSpan(previewFrame.Frame)))
            {
                if (dialog.ShowDialog() == DialogResult.OK)
                {
                    int i = Program.TimeSpanToFrame(dialog.Value);
                    i = Math.Max(0, Math.Min(Program.VideoSource.NumberOfFrames - 1, i)); // Make sure we don't go out of bounds.
                    previewFrame.Frame = i;
                }
            }
        }

        private void startToolStripMenuItem_Click(object sender, EventArgs e)
        {
            previewFrame.Frame = Filters.Trim.TrimStart;
        }

        private void endToolStripMenuItem_Click(object sender, EventArgs e)
        {
            previewFrame.Frame = Filters.Trim.TrimEnd;
        }

        private void checkTurnOnlyOne(object sender, EventArgs e)
        {
            checkTurnNormal.CheckedChanged -= checkTurnOnlyOne;
            checkTurnRight.CheckedChanged -= checkTurnOnlyOne;
            checkTurnTwice.CheckedChanged -= checkTurnOnlyOne;
            checkTurnLeft.CheckedChanged -= checkTurnOnlyOne;

            if (!ReferenceEquals(sender, checkTurnNormal))
                checkTurnNormal.Checked = false;

            if (!ReferenceEquals(sender, checkTurnRight))
                checkTurnRight.Checked = false;

            if (!ReferenceEquals(sender, checkTurnTwice))
                checkTurnTwice.Checked = false;

            if (!ReferenceEquals(sender, checkTurnLeft))
                checkTurnLeft.Checked = false;

            if (!checkTurnNormal.Checked &&
                !checkTurnRight.Checked &&
                !checkTurnTwice.Checked &&
                !checkTurnLeft.Checked)
                checkTurnNormal.Checked = true;

            checkTurnNormal.CheckedChanged += checkTurnOnlyOne;
            checkTurnRight.CheckedChanged += checkTurnOnlyOne;
            checkTurnTwice.CheckedChanged += checkTurnOnlyOne;
            checkTurnLeft.CheckedChanged += checkTurnOnlyOne;
        }

        private void buttonConfirm_Click(object sender, EventArgs e)
        {
            var mode = RotateFilter.RotateMode.None;

            if (checkTurnRight.Checked)
                mode = RotateFilter.RotateMode.Right;
            if (checkTurnTwice.Checked)
                mode = RotateFilter.RotateMode.Twice;
            if (checkTurnLeft.Checked)
                mode = RotateFilter.RotateMode.Left;

            GeneratedFilter = new RotateFilter(mode, checkFlipHorizontal.Checked, checkFlipVertical.Checked);

            DialogResult = DialogResult.OK;

            Close();
        }
    }

    public class RotateFilter
    {
        public readonly RotateMode Mode;
        public readonly bool FlipHorizontal;
        public readonly bool FlipVertical;

        public RotateFilter(RotateMode mode, bool flipHorizontal, bool flipVertical)
        {
            Mode = mode;
            FlipHorizontal = flipHorizontal;
            FlipVertical = flipVertical;
        }

        public override string ToString()
        {
            string command;

            switch (Mode)
            {
                default:
                    command = "";
                    break;
                case RotateMode.Right:
                    command = "TurnRight() ";
                    break;
                case RotateMode.Twice:
                    command = "TurnRight() TurnRight() ";
                    break;
                case RotateMode.Left:
                    command = "TurnLeft() ";
                    break;
            }

            if (FlipHorizontal)
            {
                command += "FlipHorizontal() ";
            }

            if (FlipVertical)
            {
                command += "FlipVertical()";
            }

            return command;
        }

        public enum RotateMode
        {
            None,
            Right,
            Twice,
            Left
        }
    }
}
