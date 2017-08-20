using System;
using System.Linq;
using System.Windows.Forms;
using static Pokemon_Shuffle_Save_Editor.Main;
using static Pokemon_Shuffle_Save_Editor.ToolFunctions;

namespace Pokemon_Shuffle_Save_Editor
{
    public partial class Pokathlon_Popup : Form
    {
        static Random rnd = new Random();
        public bool retEnabled
        {
            get { return CHK_Paused.Checked; }
        }
        public int retStep
        {
            get { return (int)NUP_Step.Value; }
        }
        public int retMoves
        {
            get { return (int)NUP_Moves.Value; }
        }
        public int retOpponent
        {
            get { return (int)NUP_Opponent.Value; }
        }

        public Pokathlon_Popup(int oValue, int mValue, int sValue, int oMin = 1, int mMin = 0, int sMin = 1, int oMax = 300, int mMax = 99, int sMax = 60)
        {   //don't forget to change default values here if more levels are added to Survival Mode
            InitializeComponent();
            int j = 0;
            int[] list = { oMin, mMin, sMin, oMax, mMax, sMax,oValue, mValue, sValue };
            foreach (NumericUpDown nup in new[] { NUP_Opponent, NUP_Moves, NUP_Step })
            {
                nup.Minimum = list[j];
                nup.Maximum = list[3 + j];
                nup.Value = list[6 + j];
                j++;
            }
            UpdateForm();
            CHK_Paused.Checked = (((BitConverter.ToInt16(savedata, 0xB768) >> 7) & 3) == 3);
        }

        protected override bool ProcessDialogKey(Keys keyData)  //Allows quit when Esc is pressed
        {
            if (Form.ModifierKeys == Keys.None && keyData == Keys.Escape)
            {
                this.Close();
                return true;
            }
            return base.ProcessDialogKey(keyData);
        }

        private void ValueChanged(object sender, EventArgs e)
        {
            UpdateForm();
        }

        private void UpdateForm()
        {
            B_Random.Visible = ((int)NUP_Step.Value > 0);
            PB_Opponent.Image = ResizeImage(GetMonImage(BitConverter.ToInt16(db.StagesMain, 0x50 + BitConverter.ToInt32(db.StagesMain, 0x4) * (int)NUP_Opponent.Value) & 0x7FF, true), 48, 48);
        }

        private void B_OK_Click(object sender, EventArgs e)
        {
            this.DialogResult = DialogResult.OK;
            this.Close();
        }

        private void B_Random_Click(object sender, EventArgs e)
        {   //Random() never equals its max value, hence max +1
            NUP_Opponent.Value = (ModifierKeys == Keys.Control) ? rnd.Next((int)NUP_Opponent.Minimum, (int)NUP_Opponent.Maximum + 1) : db.Pokathlon[(int)NUP_Step.Value - 1][rnd.Next(db.Pokathlon[(int)NUP_Step.Value - 1].Count)];
            //int min = (ModifierKeys == Keys.Control) ? (int)NUP_Opponent.Minimum : db.Pokathlon[(int)NUP_Step.Value - 1].Min();
            //int max = (ModifierKeys == Keys.Control) ? (int)NUP_Opponent.Maximum : db.Pokathlon[(int)NUP_Step.Value - 1].Max(); 
            //NUP_Opponent.Value = new Random().Next(min, max);
        }
    }
}