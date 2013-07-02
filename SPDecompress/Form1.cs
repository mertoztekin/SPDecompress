using System;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using System.IO;
using System.IO.Compression;
using System.Xml;
using System.Xml.Xsl;

namespace SPDecompress
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }

        private void buttonDecompress_Click(object sender, EventArgs e)
        {
            byte[] compressedByteBuffer;
            string uncompressedStringBuffer = String.Empty;

            labelStatus.Text = String.Empty;

            #region Prepare Byte Buffer

            try
            {
                if (textBoxCompressed.Text.StartsWith("0x"))
                {
                    compressedByteBuffer = ConvertHexTringToByteArray(textBoxCompressed.Text.Substring(2));
                }
                else
                {
                    compressedByteBuffer = ConvertHexTringToByteArray(textBoxCompressed.Text);
                }
            }
            catch (Exception exception)
            {
                labelStatus.Text = exception.Message;
                return;
            }

            #endregion

            #region Decompress Byte Buffer

            try
            {
                uncompressedStringBuffer = Decompress(compressedByteBuffer);
            }
            catch (Exception exception)
            {
                labelStatus.Text = exception.Message;
                return;
            }

            #endregion

            #region Display Uncompressed Info
            
            XmlDocument xmlDocument = new XmlDocument();

            xmlDocument.LoadXml("<uncompressedData></uncompressedData>");

            bool isXmlDocument = false;

            try
            {
                xmlDocument.FirstChild.InnerXml = uncompressedStringBuffer;                
                isXmlDocument = true;
            }
            catch (Exception exception)
            {
                isXmlDocument = false;
            }

            txtOuput.Text = uncompressedStringBuffer;

            if (isXmlDocument)
            {
                using (MemoryStream xslMemoryStream = new MemoryStream(Properties.Resources.DefaultStylesheet))
                {
                    XmlReader xmlReader = XmlReader.Create(xslMemoryStream);
                    XslCompiledTransform xslCompiledTransform = new XslCompiledTransform();
                    xslCompiledTransform.Load(xmlReader);
                    StringBuilder xmlBuffer = new StringBuilder();
                    XmlWriter xmlWriter = XmlWriter.Create(xmlBuffer);
                    xslCompiledTransform.Transform(xmlDocument, xmlWriter);
                    webBrowserUncompressed.DocumentText = xmlBuffer.ToString();
                }
            }
            else
            {
                webBrowserUncompressed.DocumentText = uncompressedStringBuffer;
            }

            #endregion
        }

        private string Decompress(byte[] compressedBytesBuffer)
        {
            string uncompressedString = String.Empty;

            using (MemoryStream compressedMemoryStream = new MemoryStream(compressedBytesBuffer))
            {
                compressedMemoryStream.Position += 12; // Compress Structure Header according to [MS -WSSFO2].
                compressedMemoryStream.Position += 2;  // Zlib header.

                using (DeflateStream deflateStream = new DeflateStream(compressedMemoryStream, CompressionMode.Decompress))
                {
                    using (MemoryStream uncompressedMemoryStream = new MemoryStream())
                    {
                        deflateStream.CopyTo(uncompressedMemoryStream);

                        uncompressedMemoryStream.Position = 0;

                        using (StreamReader streamReader = new StreamReader(uncompressedMemoryStream))
                        {
                            uncompressedString = streamReader.ReadToEnd();
                        }
                    }
                }
            }

            return uncompressedString;
        }


        private byte[] ConvertHexTringToByteArray(string hexString)
        {
            int hexLength = hexString.Length;

            if ((hexLength % 2) != 0)
            {
                throw new InvalidDataException("The number of characters in the sring should be even.");
            }

            byte[] hexValues = new byte[] {0x00, 0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08, 0x09, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x0A, 0x0B, 0x0C, 0x0D, 0x0E, 0x0F};

            byte[] byteBuffer = new byte[hexLength / 2];

            //e.g. ASCII value for 'A' = 65, substract ASCII value for '0' (48) => 17, locate 17th value in array.

            for (int index = 0; index < hexLength; index += 2)
            {
                byteBuffer[index / 2] = (byte)(hexValues[Char.ToUpper(hexString[index + 0]) - '0'] << 4 |  hexValues[Char.ToUpper(hexString[index + 1]) - '0']);                
            }

            return byteBuffer;
        }

        private void button1_Click(object sender, EventArgs e)
        {
            if (Clipboard.ContainsText())
            {
                textBoxCompressed.Text = String.Empty;
                textBoxCompressed.Text = Clipboard.GetText();
            }
        }

        private void txtOuput_DoubleClick(object sender, EventArgs e)
        {
            txtOuput.SelectAll();
        }
    }
}
