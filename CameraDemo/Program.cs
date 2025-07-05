// See https://aka.ms/new-console-template for more information
// 或者抓取内容


public class Camera
{
    public bool IsConnected { get; private set; } = false;
    public bool IsOpened { get; private set; } = false;

    public void Connect()
    {
        // Simulate camera connection
        IsConnected = true;
        Console.WriteLine("Camera connected.");
    }

    public void Open()
    {
        if (!IsConnected)
        {
            throw new InvalidOperationException("Camera is not connected.");
        }
        
        // Simulate opening the camera
        IsOpened = true;
        Console.WriteLine("Camera opened.");
    }

    public object CaptureImage()
    {
        if (!IsConnected)
        {
            throw new InvalidOperationException("Camera is not connected.");
        }
        if (!IsOpened)
        {
            throw new InvalidOperationException("Camera is not opened.");
        }
        
        // Simulate image capture
        return new { ImageData = "CapturedImageData" };
    }
}