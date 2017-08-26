﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using ZYNet.CloudSystem;
using ZYNet.CloudSystem.Client;

namespace Client
{
    public partial class LogOn : Form
    {
        public LogOn()
        {
            InitializeComponent();
        }


        [MethodCmdTag(1001)]
        public string GetNick(CloudClient client)
        {
            NickWin win = new NickWin();
            win.ShowDialog();
            return win.Nick;
        }

        private async void button1_Click(object sender, EventArgs e)
        {

            var res = await ClientManager.NewAsync().Get<ServerMethods>().LogOn(this.textBox1.Text);

            var isOK = res?.First?.Value<bool>();

            if(isOK!=null)
            {
                if(isOK.Value)
                {
                    this.BeginInvoke(new EventHandler(delegate
                    {
                        this.Close();
                    }));                   
                }
                else
                {
                    MessageBox.Show(res[1].Value<string>());
                }
            }

         
        }


        private void LogOn_Load(object sender, EventArgs e)
        {
            ClientManager.Client.Install(this);
        }
    }
}
