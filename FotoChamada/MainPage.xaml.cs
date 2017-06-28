using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using System.Threading.Tasks;//Threads
using Windows.UI;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using System.Windows.Input;

using Windows.Media.Capture;//Camera
using Windows.Storage;//Manipulacao do arquivo
using Windows.Storage.Streams;
using Windows.Graphics;
using Windows.UI.Xaml.Media.Imaging;
using Windows.Graphics.Imaging;


using System.Windows;
//Api de emocoes
using Microsoft.ProjectOxford.Emotion;
using Microsoft.ProjectOxford.Emotion.Contract;

//Api da face
using Microsoft.ProjectOxford.Face;
using Microsoft.ProjectOxford.Face.Contract;

using Windows.Storage.Pickers;
using Windows.UI.Xaml.Shapes;
using Microsoft.Graphics.Canvas.UI.Xaml;


namespace FotoChamada
{
    public sealed partial class MainPage : Page
    {
        CameraCaptureUI captureUI = new CameraCaptureUI();
        StorageFile photo;
        IRandomAccessStream imageStream;

        const string emotionKey = "53349a89ad7148cba0590fc965a39d96";
        const string faceKey = "f3cc67c6452240b58d000c1854b1ff98";

        EmotionServiceClient emotionService = new EmotionServiceClient(emotionKey);
        IFaceServiceClient faceServiceClient = new FaceServiceClient(faceKey, "https://westcentralus.api.cognitive.microsoft.com/face/v1.0");
        Emotion[] emotionResults;//É um array de json pois identificara todos os rostos
        FaceRectangle[] faceRects; // Json dos rostos

        public MainPage()
        {
            this.InitializeComponent();
            this.captureUI.PhotoSettings.Format = CameraCaptureUIPhotoFormat.Jpeg;
            this.captureUI.PhotoSettings.CroppedSizeInPixels = new Size(250, 250);


        }
        /*
         *Metodo responsavel por capturar uma imagem e enviar a MainPage
         * Mais informações: https://docs.microsoft.com/pt-br/windows/uwp/audio-video-camera/capture-photos-and-video-with-cameracaptureui
         */
        private async void take_photo(object sender, RoutedEventArgs e)
        {
            try
            {
                //Captura assincrona
                photo = await this.captureUI.CaptureFileAsync(CameraCaptureUIMode.Photo);

                // Se o Usuario cancelou a captura da foto
                if (photo == null)
                {

                    return;
                }
                else
                {
                    //Carrego a foto
                    this.imageStream = await photo.OpenAsync(FileAccessMode.Read);
                    BitmapDecoder decoder = await BitmapDecoder.CreateAsync(imageStream);
                    SoftwareBitmap softBitmap = await decoder.GetSoftwareBitmapAsync();

                    //Converto com as exigencias de exibição na pagina XAML
                    SoftwareBitmap softBitmapBGR8 = SoftwareBitmap.Convert(softBitmap, BitmapPixelFormat.Bgra8, BitmapAlphaMode.Premultiplied);
                    SoftwareBitmapSource bitmapSource = new SoftwareBitmapSource();
                    await bitmapSource.SetBitmapAsync(softBitmapBGR8);

                    //Anexo ao campo "image" a foto armazenada
                    image.Source = bitmapSource;
                }


            }
            catch
            {
                //Envio a mensagem de erro pelo campo text que criei na tela
                output.Text = "Erro: taking photo";
            }
        }

        private async void getEmotion_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                emotionResults = await emotionService.RecognizeAsync(imageStream.AsStream());
                faceRects = await UploadAndDetectFaces(photo.Path, photo);

                //Desenho um quadrado nas posicoes identificadas do rosto
                var property = await photo.Properties.GetImagePropertiesAsync();
                var writeableBmp = BitmapFactory.New((int)property.Width, (int)property.Height);

                using (writeableBmp.GetBitmapContext())
                {
                    //Load bitmap from image file
                    using (var fileStream = await photo.OpenAsync(FileAccessMode.Read))
                    {
                        writeableBmp = await BitmapFactory.New(1, 1).FromStream(fileStream, BitmapPixelFormat.Bgra8);
                    }
                }
                if (faceRects != null && faceRects.Length > 0)
                {
                    foreach (var face in faceRects)
                    {
                        writeableBmp.DrawRectangle(face.Left, face.Top,
                        face.Left + face.Width,
                        face.Top + face.Height, Colors.Yellow);
                    }
                    debug.Text = "Rostos identificados: " + faceRects.Length;

                }
                //Sobrescreve com uma nova contendo as marcacoes
                image.Source = writeableBmp;

                output.Text = "";
                if (emotionResults != null)
                {
                    int i = 0;
                    foreach (var p in emotionResults)
                    {
                        
                        var score = p.Scores;
                        output.Text += "Your Emotions are for photo #" + i + "  : \n" +

                         "Feliz: " + String.Format("{0:0.##}", score.Happiness * 100) + " %" + "\n" +

                         "Tristeza: " + String.Format("{0:0.##}", score.Sadness * 100) + " %" + "\n" +

                         "Surpreso: " + String.Format("{0:0.##}", score.Surprise * 100) + " %" + "\n" +

                         "Raiva: " + String.Format("{0:0.##}", score.Anger * 100) + " %" + "\n" +

                         "Desprezo: " + String.Format("{0:0.##}", score.Contempt * 100) + " %" + "\n" +

                         "Desgosto: " + String.Format("{0:0.##}", score.Disgust * 100) + " %" + "\n" +

                         "Medo: " + String.Format("{0:0.##}", score.Fear * 100) + " %" + "\n" +

                         "Neutro: " + String.Format("{0:0.##}", score.Neutral * 100) + " %" + "\n";
                        i++;
                    }


                }
            }
            catch (Exception ex)
            {
                output.Text = "Erro: Check Emotions \n" + ex.Message + "\n";
            }


        }
        /*
         * Metodo para obter uma foto local do computador
         * 
         */
        private async void getPhotoLocal_Click(object sender, RoutedEventArgs e)
        {

            FileOpenPicker open = new FileOpenPicker();
            open.FileTypeFilter.Add(".jpg");
            open.FileTypeFilter.Add(".jpeg");
            
            
            // Open a stream for the selected file 
            photo = await open.PickSingleFileAsync();

            // Verifica se carregou algo 
            if (photo != null)
            {
                imageStream = await photo.OpenAsync(FileAccessMode.Read);
                BitmapDecoder decoder = await BitmapDecoder.CreateAsync(imageStream);
                SoftwareBitmap softBitmap = await decoder.GetSoftwareBitmapAsync();

                //Converto com as exigencias de exibição na pagina XAML
                SoftwareBitmap softBitmapBGR8 = SoftwareBitmap.Convert(softBitmap, BitmapPixelFormat.Bgra8, BitmapAlphaMode.Premultiplied);
                SoftwareBitmapSource bitmapSource = new SoftwareBitmapSource();
                await bitmapSource.SetBitmapAsync(softBitmapBGR8);

                //Anexo ao campo "image"
                image.Source = bitmapSource;


            }
            else
            {
                output.Text = "Cancelou o envio de imagem";
            }
        }

        /*
            * Faz o upload da imagem para a API de reconheicmento de rostos
            * O retorno é a quantidade de rostos identificados. 
        */
        private async Task<FaceRectangle[]> UploadAndDetectFaces(string imageFilePath, StorageFile storage)
        {
            try
            { 
                StorageFile storageFile = storage;

                var randomAccessStream = await storageFile.OpenReadAsync();
                using (Stream imageFileStream = randomAccessStream.AsStreamForRead())
                {
                    var faces = await faceServiceClient.DetectAsync(imageFileStream);
                    var faceRects = faces.Select(face => face.FaceRectangle);
                    //output.Text = "Faces: {0}" + String.Format("{0}", faceRects.ToArray().Length);
                    return faceRects.ToArray();
                }
            }
            catch (Exception ex)
            {
                output.Text = "Excepiton: " + String.Format("{0}", ex.Source);
                System.Diagnostics.Debug.WriteLine(ex.Message);
            }
            {
                return new FaceRectangle[0];// nao identificou nada
            }
        }

      
    }
}
