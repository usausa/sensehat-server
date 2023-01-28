using Iot.Device.SenseHat;

#pragma warning disable CA1852

using var senseHat = new SenseHat();
Console.WriteLine(senseHat.Temperature.Value);
Console.WriteLine(senseHat.Temperature2.Value);
Console.WriteLine(senseHat.TemperatureAndHumidity.Temperature.Value);
Console.WriteLine(senseHat.TemperatureAndHumidity.Humidity.Value);
