using System;

using System.Net.NetworkInformation;
using System.Text;
using System.Windows.Threading;
using System.Windows;
using System.Threading;
using System.ComponentModel;
using System.Speech.Synthesis;
using System.Collections.Generic;
using System.Windows.Controls.Primitives;
using System.Windows.Controls;

namespace DoPing
{
  
   public class error
    {
       public string returnMessage { get; set; }
        public int bg_status { get; set; }
       public bool is_alarm_on { get; set; }
        public bool user_stopped_the_alarm { get; set; }
        public bool is_ttl_checked ;
        public bool is_rtt_checked ;
        public bool is_len_checked ;
        public int network_gone;
        public int max_packet_lost_count;
        public DateTime ? last_timeout;
        public List<string> packet_lost_error_list = new List<string>(30);
        public error()
        {
            is_len_checked = false;
            is_rtt_checked = true;
            is_ttl_checked = false;
            returnMessage = "";
            bg_status = 0;
            is_alarm_on = false;
            user_stopped_the_alarm = false;
            network_gone = 0;
            max_packet_lost_count = 3;
            packet_lost_error_list.Clear();
        }


    }

    public partial class MainWindow : Window
    {
        string hostname = "192.168.3.10";
        int timeout = 3000;
        string data = "somedummydata";
        bool has_ping_started = false;
        bool is_reset = false;
        SpeechSynthesizer synth = new SpeechSynthesizer();

      
        PingOptions options = new PingOptions(64, true);
        DispatcherTimer timer = new DispatcherTimer();
        BackgroundWorker bg = new BackgroundWorker();

        error r_obj = new error();
        bool win_closed_clicked=false;
        Dictionary<int, string> custom_error_sound = new Dictionary<int, string>();
        public MainWindow()
        {
            InitializeComponent();
     
           
            stop_alarm_btn.Visibility = Visibility.Hidden;

            txt1.Text = hostname;
            custom_error_sound.Add(1, "request is timed out");
            custom_error_sound.Add(2, "ttl reached 0");
            custom_error_sound.Add(3, "packet size is too long");
            custom_error_sound.Add(4, "no resources");
            custom_error_sound.Add(5, "ping is prohibated");

            mtu_box.Text = "32";
            timeout_box.Text ="3";
            notify_box.Text = r_obj.max_packet_lost_count.ToString();
            bg.DoWork += new DoWorkEventHandler(PingMe);
            bg.RunWorkerCompleted += new RunWorkerCompletedEventHandler(bw_RunWorkerCompleted);
            bg.WorkerSupportsCancellation = true;

           


        }

       



        private void bw_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            if (list1.Items.Count == 21)
                list1.Items.Clear();
            list1.Items.Add(((error)e.Result).returnMessage);
            list1.SelectedIndex = list1.Items.Count - 1;
            list1.ScrollIntoView(list1.SelectedIndex);
            r_obj.returnMessage = string.Empty;
            bool is_alarm_on= ((error)e.Result).is_alarm_on;
            if(is_reset)
            {

                list1.Items.Add("Thread cancelled");
                return;
            }
            if(is_alarm_on && r_obj.network_gone>r_obj.max_packet_lost_count)
                stop_alarm_btn.Visibility = Visibility.Visible;
            else
                stop_alarm_btn.Visibility = Visibility.Hidden;

            if (r_obj.last_timeout!=null)
            {
                lst_time_out.Text = r_obj.last_timeout.ToString() +" "+r_obj.network_gone+" "+"times";
            }

            if (!win_closed_clicked)
            bg.RunWorkerAsync(r_obj);
        }

        private  void PingMe(object sender, DoWorkEventArgs e)
        {

            

            Thread.Sleep(1500);
            byte[] buffer = Encoding.ASCII.GetBytes(data);
            string returnMessage= string.Empty;
            int status = 0;
            bool user_stopped_alarm = r_obj.user_stopped_the_alarm;

        Ping ping = new Ping();
            
            try
            {
                PingReply reply = ping.Send(hostname, timeout,buffer,options);

                if (reply.Status == IPStatus.Success)
                {
                    returnMessage = "reply from" + " " + reply.Address;
                    if (r_obj.is_len_checked)
                       returnMessage= returnMessage + " "+"bytes=" + " " + reply.Buffer.Length; 
                    if(r_obj.is_rtt_checked)
                        returnMessage = returnMessage +" "+ "time=" + " " + reply.RoundtripTime+"ms";

                    if (r_obj.is_ttl_checked)
                        returnMessage = returnMessage + " "+ "ttl=" + " " + reply.Options.Ttl;
                    //list1.Items.Add(returnMessage);
                    status = 0;
                    r_obj.is_alarm_on = false;
                    if (r_obj.last_timeout != null)
                    {
                        r_obj.packet_lost_error_list.Add("timed out at" + " " + r_obj.last_timeout.ToString() + " " + r_obj.network_gone + " " + "times".ToString());
                        r_obj.last_timeout = null;

                    }
                    r_obj.network_gone = 0;


                }
                else if(reply.Status == IPStatus.TimedOut)
                {
                   returnMessage= "time out at"+" " + DateTime.Now.ToLocalTime().ToString() ;
                    status = 1;
                    r_obj.network_gone++;
                    r_obj.last_timeout = DateTime.Now.ToLocalTime();
                  //  r_obj.packet_lost_error_list.Add("timed out at"+" "+ DateTime.Now.ToLongTimeString().ToString());

                }
                else if (reply.Status == IPStatus.TimeExceeded)
                {
                    returnMessage="TTL reaches at 0" +" "+ DateTime.Now.ToLocalTime().ToString();
                    status = 2;
                    r_obj.network_gone++;
                    r_obj.last_timeout = DateTime.Now.ToLocalTime();
                  //  r_obj.packet_lost_error_list.Add("timed out at" + " " + DateTime.Now.ToLongTimeString().ToString());


                }
                else if (reply.Status == IPStatus.PacketTooBig)
                {
                    returnMessage="packet too big" + DateTime.Now.ToLocalTime().ToString();

                    status = 3;
                    r_obj.network_gone++;

                    r_obj.last_timeout = DateTime.Now.ToLocalTime();
                   // r_obj.packet_lost_error_list.Add("timed out at" + " " + DateTime.Now.ToLongTimeString().ToString());

                }

                else if (reply.Status == IPStatus.NoResources)
                {
                    returnMessage="no resource....." + DateTime.Now.ToLocalTime().ToString();
                    status = 4;
                    r_obj.network_gone++;
                    r_obj.last_timeout = DateTime.Now.ToLocalTime();
                   // r_obj.packet_lost_error_list.Add("timed out at" + " " + DateTime.Now.ToLongTimeString().ToString());


                }

                else if (reply.Status == IPStatus.DestinationProhibited)
                {
                    returnMessage="ping is prohibated" + DateTime.Now.ToLocalTime().ToString();
                    status = 5;
                    r_obj.network_gone++;
                    r_obj.last_timeout = DateTime.Now.ToLocalTime();

                  //  r_obj.packet_lost_error_list.Add("timed out at" + " " + DateTime.Now.ToLongTimeString().ToString());

                }


                else
                {
                   returnMessage =  reply.Status.ToString();
                    status = 99;
                    r_obj.network_gone++;
                    r_obj.last_timeout = DateTime.Now.ToLocalTime();
                  //  r_obj.packet_lost_error_list.Add("timed out at" + " " + DateTime.Now.ToLongTimeString().ToString());

                }

                r_obj.returnMessage = returnMessage;
                r_obj.bg_status = status;
                e.Result = r_obj;
                if (status == 99 && !user_stopped_alarm && r_obj.network_gone>r_obj.max_packet_lost_count)
                {
                    synth.Speak(returnMessage);
                    r_obj.is_alarm_on = true;

                }
                else if (status != 0 && !user_stopped_alarm && r_obj.network_gone > r_obj.max_packet_lost_count)
                {
                    synth.Speak(custom_error_sound[status]);
                    r_obj.is_alarm_on = true;

                }



            }
            catch (PingException ex)
            {
                returnMessage = ex.Message;
                r_obj.returnMessage = returnMessage;
                r_obj.bg_status = 100;
                r_obj.is_alarm_on = true;
                r_obj.network_gone++;
                e.Result = r_obj;
                r_obj.last_timeout = DateTime.Now.ToLocalTime();
             //   r_obj.packet_lost_error_list.Add("timed out at" + " " + DateTime.Now.ToLongTimeString().ToString());

                if (!user_stopped_alarm && r_obj.network_gone > r_obj.max_packet_lost_count)
                synth.Speak(r_obj.returnMessage);


            }
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
           
            int mtu = 0;
            int timeout=0;
            int max_p_lost_count = r_obj.max_packet_lost_count;
            if (mtu_box.Text.Length > 0)
            {
                try
                {
                     mtu = Convert.ToInt32(mtu_box.Text);
                    if (mtu < 0)
                    {
                        list1.Items.Add("packet size can not be negative");
                        return;

                    }
                    else if (mtu > 65535)
                    {
                        list1.Items.Add("packet size should bnot be greater than 65535 byte");
                        return;

                    }
                    else
                    {
                        char[] b = new char[mtu];
                        String buffer_msg = new string(b);
                        data = buffer_msg;

                    }

                }
                catch
                {
                    list1.Items.Add("packet size shoud be an integer");
                    return;
                }

               
                // 
            }

            if (timeout_box.Text.Length > 0)
            {
                try
                {
                    timeout = Convert.ToInt32(timeout_box.Text);
                    if (timeout < 0)
                    {
                        list1.Items.Add("reply timeout period can not be negative");
                        return;

                    }
                    else if (timeout > 10)
                    {


                        list1.Items.Add("reply timeout period is too long. should be less than 10 sec.");
                        return;

                    }
                    else
                    {
                        this.timeout = timeout;

                    }

                }
                catch
                {
                    list1.Items.Add("reply timeout period shoud be an integer");
                    return;

                }


                // 
            }

            if (notify_box.Text.Length > 0)
            {
                try
                {
                    max_p_lost_count = Convert.ToInt32(notify_box.Text);
                    if (max_p_lost_count < 0)
                    {
                        list1.Items.Add("packet lost notifying counter can not be negative");
                        return;

                    }
                  
                    else
                    {
                        r_obj.max_packet_lost_count = max_p_lost_count;

                    }

                }
                catch
                {
                    list1.Items.Add("packet lost notifying counter should be an integer");
                    return;

                }


                // 
            }


            if (txt1.Text.Length>0)
            {
                hostname = txt1.Text;
            }

           if(!bg.IsBusy)
            {
                ping_btn.IsEnabled = false;
                is_reset = false;
                txt1.IsEnabled = false;
                has_ping_started = true;
                list1.Items.Add("pinging started with message size" + " " + data.Length + " " + "and timeout" + " " + timeout);
                list1.Items.Add("we will notify you if continiously"+" "+r_obj.max_packet_lost_count+" "+" packet is lost");
                bg.RunWorkerAsync();
            }
           else
            {
               
                synth.Speak("background thread is busy please open new instance");
            }
        }

        private void win_Loaded(object sender, RoutedEventArgs e)
        {
            
        }

        private void ttl_chkbox_Checked(object sender, RoutedEventArgs e)
        {
           
               r_obj.is_ttl_checked = true;
          

            
        }

        private void rtt_chkbox_Checked(object sender, RoutedEventArgs e)
        {
            r_obj.is_rtt_checked = true;
           
        }

        private void len_chkbox_Checked(object sender, RoutedEventArgs e)
        {

            r_obj.is_len_checked = true;
       

        }


      

        private void ttl_chkbox_Unchecked(object sender, RoutedEventArgs e)
        {
            r_obj.is_ttl_checked = false;

        }

        private void rtt_chkbox_Unchecked(object sender, RoutedEventArgs e)
        {
            r_obj.is_rtt_checked = false;

        }

        private void len_chkbox_Unchecked(object sender, RoutedEventArgs e)
        {
            r_obj.is_len_checked = false;

        }

        private void Button_Click_1(object sender, RoutedEventArgs e)
        {
            MainWindow win = new MainWindow();
            win.Show();
        }

        private void stop_alarm_btn_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("alarm is stopped. now reset so that it will notify you again");
            r_obj.user_stopped_the_alarm = true;
        }

        private void win_Closing(object sender, CancelEventArgs e)
        {
            bg.CancelAsync();
            win_closed_clicked = true;
            
        }

        private void Button_Click_2(object sender, RoutedEventArgs e)
        {//reset function
            if (has_ping_started)
            {
                txt1.IsEnabled = true;
                this.data = string.Empty;
                is_reset = true;
                this.timeout = 3000;
                r_obj = new error();
                r_obj.packet_lost_error_list.Clear();
                ping_btn.IsEnabled = true;
                list1.Items.Clear();
                has_ping_started = false;
            }
            else
            {
                MessageBox.Show("pinging has not been started yet");
            }
        }

        private void Button_Click_3(object sender, RoutedEventArgs e)
        {
            //history button

           
                history win2 = new history();
            if (r_obj.packet_lost_error_list.Count ==0)
            {
                win2.history_list.Items.Add("NO HISTORY FOUND");
            }
            else
            {
                foreach(string s in r_obj.packet_lost_error_list)
                {
                    win2.history_list.Items.Add(s);

                }
            }

            win2.Show();
            
        }
    }
}
