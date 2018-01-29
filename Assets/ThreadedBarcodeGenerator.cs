using UniRx;
using UnityEngine;
using UnityEngine.UI;
using ZXing;
using ZXing.Common;

[RequireComponent(typeof(RawImage))]
public class ThreadedBarcodeGenerator : MonoBehaviour
{
    [SerializeField] private BarcodeFormat format = BarcodeFormat.QR_CODE;
    [SerializeField] private string data = "test";
    [SerializeField] private int width = 512;
    [SerializeField] private int height = 512;

    private RawImage cRawImage;

    private void Start()
    {
        cRawImage = GetComponent<RawImage>();

        // Note that there is an initial spike here, when UniRx creates the MainThreadDispatcher, which is done only
        // once, when you first dispatch somethign to the main thread. If you want to profile the code, just run
        // Observable.Start(() => { }).Subscribe() before
        GenerateBarcode(data, format, width, height)
            .Subscribe(tex =>
            {
                // Setup the RawImage
                cRawImage.texture = tex;
                cRawImage.rectTransform.sizeDelta = new Vector2(tex.width, tex.height);
            }).AddTo(this);
    }

    private IObservable<Texture2D> GenerateBarcode(string data, BarcodeFormat format, int width, int height)
    {
        return Observable.Start(() =>
            {
                // Generate the BitMatrix
                BitMatrix bitMatrix = new MultiFormatWriter()
                    .encode(data, format, width, height);

                // Generate the pixel array
                Color[] pixels = new Color[bitMatrix.Width * bitMatrix.Height];
                int pos = 0;
                for (var y = 0; y < bitMatrix.Height; y++)
                {
                    for (var x = 0; x < bitMatrix.Width; x++)
                    {
                        pixels[pos++] = bitMatrix[x, y] ? Color.black : Color.white;
                    }
                }

                return new Tuple<int[], Color[]>(new[] {bitMatrix.Width, bitMatrix.Height}, pixels);
            }, Scheduler.ThreadPool)
            .ObserveOnMainThread() // The texture itself needs to be created on the main thread.
            .Select(res =>
            {
                Texture2D tex = new Texture2D(res.Item1[0], res.Item1[1]);
                tex.SetPixels(res.Item2);
                tex.Apply();
                return tex;
            });
    }
}