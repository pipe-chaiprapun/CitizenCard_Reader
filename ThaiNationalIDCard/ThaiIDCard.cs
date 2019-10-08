/* BSD license
 * Credit:  APDU Command from Mr.Manoi http://hosxp.net/index.php?option=com_smf&topic=22496
 * Require add reference: PresentationCore, System.Xaml, WindowsBase
 * Require add refrernce(Nuget): PCSC ( http://www.nuget.org/packages/PCSC/ )
 */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using PCSC;
using System.Windows.Forms;
using System.Threading;

namespace ThaiNationalIDCard
{
    public delegate void handlePhotoProgress(int value, int maximum);
    public delegate void handleCardInserted(Personal personal);
    public delegate void handleCardReadError(string message);
    public delegate void handleBeforeCardInserted();
    public delegate void handleReaderDisconnected();
    public delegate void handleCardRemoved();

    public class ThaiIDCard
    {
        #region constant
        const int ECODE_SCardError = 256;
        const int ECODE_UNSUPPORT_CARD = 1;
        #endregion

        #region members
        private SCardReader _cardReader;
        private SCardError _err;
        private IntPtr _pioSendPci;
        private SCardContext _context;
        private IAPDU_THAILAND_IDCARD _apdu;
        private SCardMonitor _monitor;
        private Progress pg = new Progress();
        private string id = null;
        private byte[] pbtemp = new byte[256];
        private int attempt=0;

        private string _error_message;
        private int _error_code;

        public event handlePhotoProgress eventPhotoProgress;
        public event handleCardInserted eventCardInserted;
        public event handleCardRemoved eventCardRemoved;
        public event handleCardReadError eventCardReadError;
        public event handleBeforeCardInserted eventBeforeCardInserted;
        public event handleReaderDisconnected eventReaderDisconnected;

        #endregion
        
        #region private method
        private bool IsExclusiveState(SCRState state)
        {
            return state.ToString().Contains(SCRState.Exclusive.ToString());
        }

        private void OnStatusChanged(StatusChangeEventArgs args)
        {
            if (args.NewState != SCRState.Present
                || args.NewState == SCRState.InUse
                || args.NewState == SCRState.Unpowered
                || args.NewState == SCRState.Mute
                || args.NewState == SCRState.Unavailable
                || IsExclusiveState(args.LastState)
                || IsExclusiveState(args.NewState))
                return;
            ReadCard();
        }

        private byte[] SendCommand(byte[][] commands)
        {
            byte[] pbRecvBuffer;
            pbRecvBuffer = new byte[256];
            foreach (byte[] command in commands)
            {
                pbRecvBuffer = new byte[256];
                _err = _cardReader.Transmit(_pioSendPci, command, ref pbRecvBuffer);
            }
            return pbRecvBuffer;
        }

        private byte[] SendPhotoCommand()
        {
            var s = new System.IO.MemoryStream();
            bool check_error;
            CMD_PAIR[] cmds_photo = _apdu.GET_CMD_CARD_PHOTO();
            for (int i = 0; i < cmds_photo.Length; i++)
            {
                byte[] recv1 = new byte[256];
                _err = _cardReader.Transmit(_pioSendPci, cmds_photo[i].CMD1, ref recv1);
                check_error = CheckErr(_err);

                if (recv1.Length > 0)
                {
                    byte[] recv2 = new byte[256];
                    _err = _cardReader.Transmit(_pioSendPci, cmds_photo[i].CMD2, ref recv2);
                    check_error = CheckErr(_err);
                    //s.Write(recv2, 0, xwd);
                    s.Write(recv2, 0, recv2.Length - 2);
                }
                if (check_error == false)
                {
                    return null;
                }

                if (eventPhotoProgress != null)
                    eventPhotoProgress(i + 1, cmds_photo.Length);
            }

            s.Seek(0, SeekOrigin.Begin);
            Console.WriteLine("Read photo complete!");
            return s.ToArray();
        }
        private byte[] SendPhotoCommand2()
        {
            var s = new System.IO.MemoryStream();
            bool check_error;
            CMD_PAIR[] cmds_photo = _apdu.GET_CMD_CARD_PHOTO();
            for (int i = 0; i < cmds_photo.Length; i++)
            {
                byte[] recv1 = new byte[256];
                _err = _cardReader.Transmit(_pioSendPci, cmds_photo[i].CMD1, ref recv1);
                check_error = CheckErr(_err);

                if (recv1.Length > 0)
                {
                    byte[] recv2 = new byte[256];
                    _err = _cardReader.Transmit(_pioSendPci, cmds_photo[i].CMD2, ref recv2);
                    check_error = CheckErr(_err);
                    //s.Write(recv2, 0, xwd);
                    s.Write(recv2, 0, recv2.Length - 2);
                }
                if (check_error == false)
                {
                    return null;
                }
            }

            s.Seek(0, SeekOrigin.Begin);
            Console.WriteLine("Read photo complete2!");
            return s.ToArray();
        }

        private string GetUTF8FromAsciiBytes(byte[] ascii_bytes)
        {
            byte[] utf8;
            utf8 = Encoding.Convert(
                Encoding.GetEncoding("TIS-620"),
                Encoding.UTF8,
                ascii_bytes
                );
            //utf8 = Encoding.Convert(
            //    Encoding.GetEncoding("windows-874"),
            //    Encoding.UTF8,
            //    ascii_bytes
            //    );
            string result = System.Text.Encoding.UTF8.GetString(utf8);
            return result.Substring(0, result.Length - 2);
        }

        private bool IsThailandSupportCard()
        {
            string[] readerNames;
            SCardProtocol proto;
            SCardState state;
            byte[] atr;
            var isSupportCard = true;

            var sc = _cardReader.Status(
                out readerNames,    // contains the reader name(s)
                out state,          // contains the current state (flags)
                out proto,          // contains the currently used communication protocol
                out atr);           // contains the ATR

            if (atr == null || atr.Length < 2)
                isSupportCard = false;

            if (atr[0] == 0x3B && atr[1] == 0x68)       //Smart card tested with old type (Figure A.)
                _apdu = new APDU_THAILAND_IDCARD_3B68();
            else if (atr[0] == 0x3B && atr[1] == 0x78)   //Smart card tested with new type (figure B.) 
                _apdu = new APDU_THAILAND_IDCARD_3B68();
            else if (atr[0] == 0x3B && atr[1] == 0x67)
                _apdu = new APDU_THAILAND_IDCARD_3B67();
            else if (atr[0] == 0x3B && atr[1] == 0x79)
                _apdu = new APDU_THAILAND_IDCARD_3B68();
            else
            {
                _error_code = ECODE_UNSUPPORT_CARD;
                _error_message = "Card not support";
                isSupportCard = false;
            }

            return isSupportCard;
        }

        private Boolean ReleaseContext()
        {
            try
            {
                _context.Release();
                return true;
            }
            catch (PCSCException ex)
            {
                _error_code = ECODE_SCardError;
                _error_message = "Release Err: " + ex.Message + " (" + ex.SCardError.ToString() + ")";
                Console.WriteLine(_error_message);

                //if (eventCardReadError != null)
                //    eventCardReadError(_error_message);

                return false;
            }
        }

        private void SetIntPtr(SCardProtocol protocol)
        {
            switch (protocol)
            {
                case SCardProtocol.T0:
                    _pioSendPci = SCardPCI.T0;
                    break;
                case SCardProtocol.T1:
                    _pioSendPci = SCardPCI.T1;
                    break;
                default:
                    _pioSendPci = new IntPtr();
                    break;
            }
        }
        #endregion

        #region public method

        // Create new SCardContext then connect, and read data+photo from card
        public bool ReadCard()
        {
            try
            {
                _context = new SCardContext();
                _context.Establish(SCardScope.System);
                _cardReader = new SCardReader(_context);
                Console.WriteLine("Connection was establish");
                var readers = _context.GetReaders();

                if (readers == null)
                {
                    MonitorStop();
                    ReleaseContext();
                    return false;
                }

                // this will invoke StatusChanged with NewState 'SCRState.Exclusive'. so, handle it carefully
                _err = _cardReader.Connect(readers.First(),
                                    SCardShareMode.Exclusive,
                                    SCardProtocol.T0 | SCardProtocol.T1);
                SetIntPtr(_cardReader.ActiveProtocol);
                Console.WriteLine("Card Reading.");
                Console.WriteLine("Check Card Error => " + _err);
                Console.WriteLine("Check Card Support => " + IsThailandSupportCard());
                //Console.WriteLine(IsThailandSupportCard());
                if (_err == SCardError.Success && IsThailandSupportCard())
                {
                    Personal personal = new Personal();
                    Console.WriteLine("inside if");
                    if (eventBeforeCardInserted != null)
                        eventBeforeCardInserted();

                    // Send SELECT/RESET command
                    SendCommand(_apdu.CMD_SELECT());

                    // CID
                    personal.Citizenid = GetUTF8FromAsciiBytes(SendCommand(_apdu.CMD_CID()));
                    id = personal.Citizenid;

                    // Fullname Thai + Eng + BirthDate + Sex
                    personal.Info = GetUTF8FromAsciiBytes(SendCommand(_apdu.CMD_PERSON_INFO()));

                    // Address
                    personal.Address = GetUTF8FromAsciiBytes(SendCommand(_apdu.CMD_ADDRESS()));

                    // issue/expire
                    personal.Issue_Expire = GetUTF8FromAsciiBytes(SendCommand(_apdu.CMD_CARD_ISSUE_EXPIRE()));

                    // get Photo
                    personal.PhotoRaw = SendPhotoCommand();
                    if (personal.PhotoRaw == null)
                    {
                        for(int i = 0; i < 3; i++)
                        {
                            Console.WriteLine("try to read photo attemp => " + (i + 1));
                            personal.PhotoRaw = SendPhotoCommand();
                            if(personal.PhotoRaw != null)
                            {
                                break;
                            }
                        }
                    }



                    Console.WriteLine("Read complete.");

                    if (eventCardInserted != null)
                        eventCardInserted(personal);

                    return true;
                }
                return false;
            }
            catch (PCSCException ex)
            {
                _error_code = ECODE_SCardError;
                _error_message = "Err: " + ex.Message + " (" + ex.SCardError.ToString() + ")";
                Console.WriteLine(_error_message);
                Console.WriteLine("Cant Read");
                if (eventCardReadError != null)
                    eventCardReadError(_error_message);

                return false;
            }
            finally
            {
                ReleaseContext();
            }
        }

        // attach StatusChanged, MonitorException event with a new Thread
        public bool MonitorStart(string readerName)
        {
            try
            {
                _monitor = new SCardMonitor(new SCardContext(), SCardScope.System);
                _monitor.StatusChanged += (sender, args) => OnStatusChanged(args);
                _monitor.CardRemoved += (sender, args) =>
                {
                    if (eventCardRemoved != null)
                        eventCardRemoved();
                };
                _monitor.MonitorException += (sender, args) =>
                {
                    if (args.SCardError == SCardError.NoReadersAvailable)
                    {
                        if (eventReaderDisconnected != null)
                            eventReaderDisconnected();
                    }
                };
                _monitor.Start(readerName);
                return true;
            }
            catch (PCSCException ex)
            {
                _error_code = ECODE_SCardError;
                _error_message = "Start ! Err: " + ex.Message + " (" + ex.SCardError.ToString() + ")";
                Console.WriteLine(_error_message);
                return false;
            }
        }

        public bool MonitorStop()
        {
            try
            {
                if (_monitor != null)
                    _monitor.Cancel();
                return true;

            }
            catch (PCSCException ex)
            {
                _error_code = ECODE_SCardError;
                _error_message = "Stop ! Err: " + ex.Message + " (" + ex.SCardError.ToString() + ")";
                Console.WriteLine(_error_message);
                return false;
            }
        }

        // Create new SCardContext then Return available Readers then close connection
        public string[] GetReaders()
        {
            try
            {
                _context = new SCardContext();
                _context.Establish(SCardScope.System);
                string[] szReaders = _context.GetReaders();
                _context.Release();
                if (szReaders == null || szReaders.Length <= 0)
                    throw new PCSCException(SCardError.NoReadersAvailable,
                        "Could not find any Smartcard reader.");
                return szReaders;
            }
            catch (PCSCException ex)
            {
                return null;
            }
        }
        private bool CheckErr(SCardError _err)
        {
            if (_err != SCardError.Success)
            {
                Console.WriteLine("ERROR");
                return false;
            }
            else
            {
                return true;
            }
            
                //throw new PCSCException(_err,
                //    SCardHelper.StringifyError(_err));
        }

        #endregion

    }
}
