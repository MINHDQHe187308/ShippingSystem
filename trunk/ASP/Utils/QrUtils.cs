using ASP.BaseCommon;
using System.Drawing;
using System.Text;
using ThoughtWorks.QRCode.Codec;

namespace ASP.Utils
{
    public sealed class QrUtils
    {
        private static readonly Lazy<QrUtils> lazy = new Lazy<QrUtils>(() => new QrUtils());
        private QRCodeEncoder _encoder = new QRCodeEncoder() { QRCodeEncodeMode = QRCodeEncoder.ENCODE_MODE.BYTE, QRCodeScale = 4, QRCodeErrorCorrect = QRCodeEncoder.ERROR_CORRECTION.M };

        private StringBuilder _builder = new StringBuilder();
        private MemoryStream _stream = new MemoryStream();

        public static QrUtils Instance { get { return lazy.Value; } }
        public QrUtils() { }

        private void ClearBuilder() { _builder.Length = 0; }

        private void ClearStream()
        {
            _stream.Flush();
            _stream.Position = 0;
            _stream.SetLength(0);
        }

        private byte[] ConvertBitmapToByArray(Bitmap img)
        {
            try
            {
                img.Save(_stream, System.Drawing.Imaging.ImageFormat.Bmp);

                return _stream.ToArray();
            }
            catch (Exception e)
            {
                return null;
            }
        }

        public byte[] GenerateQRProcessCode(string pCode, int tagNo, int tmonth, int tyear, int ltype, string group)
        {
            try
            {
                ClearBuilder();
                ClearStream();
                _encoder.QRCodeVersion = 0;
                _encoder.QRCodeErrorCorrect = QRCodeEncoder.ERROR_CORRECTION.M;

                _builder.Append("TWPIS");
                _builder.Append(tmonth.ToString("00"));
                _builder.Append(tyear.ToString());
                _builder.Append(pCode.PadRight(10, ' '));
                _builder.Append(tagNo.ToString("00000"));
                _builder.Append(ltype.ToString("000"));

                // do dai chuoi giong nhau de kich thuoc QR sau scale giong nhau
                if (ltype == (int)EnumLocationType.MIX)
                {
                    _builder.Append(group.PadLeft(27, ' '));
                }
                else
                {
                    group = "";
                    _builder.Append(group.PadLeft(27, ' '));
                }

                var imgQR = _encoder.Encode(_builder.ToString(), Encoding.UTF8);

                return ConvertBitmapToByArray(imgQR);
            }
            catch (Exception)
            {
                return null;
            }
        }
    }
}
