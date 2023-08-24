using Microsoft.ML;
using Microsoft.ML.Data;
using StrømAPI.Models;

namespace StrømAPI;

public class PricePredictorService
{
    private MLContext _mlContext;
    private Timer _timer;
    private HourlyPriceDB _dbContext;
    private ITransformer? _model;

    public PricePredictorService(HourlyPriceDB db)
    {
        _mlContext = new MLContext();
        _timer = new Timer(RetrainModel);
        _dbContext = db;
    }
    public static float GetUnixTimestamp(DateOnly date, TimeOnly time)
    {
        DateTime combinedDateTime = date.ToDateTime(time);
        DateTime epochDateTime = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        TimeSpan timeSpan = combinedDateTime.ToUniversalTime() - epochDateTime;
        return (float)timeSpan.TotalSeconds;
    }

    private HourlyPriceTrainer[] LoadDataFromDb()
    {
        var data = _dbContext.Prices.ToArray();
        HourlyPriceTrainer[] dataOut = new HourlyPriceTrainer[data.Length];
        for (int i = 0;i<data.Length;i++)
        {
            dataOut[i] = new HourlyPriceTrainer(GetUnixTimestamp(data[i].Date, data[i].Time), (float)data[i].Price,
                data[i].Area);
        }
        return dataOut;
    }

    public void Initialize()
    {
        var data = LoadDataFromDb();

        Console.WriteLine($"Test price: {data[0].Price}");

        TrainModel(data);

        _timer.Change(1209600, Timeout.Infinite);

        Console.WriteLine("Price prediction model trained, retraining task scheduled");
    }

    private void TrainModel(HourlyPriceTrainer[] data)
    {
        var dataView = _mlContext.Data.LoadFromEnumerable(data);

        var dataProcessPipeline =
            _mlContext.Transforms.CopyColumns(outputColumnName: "Label", inputColumnName: nameof(HourlyPriceTrainer.Price))
                .Append(_mlContext.Transforms.Categorical.OneHotEncoding(outputColumnName: "Area", inputColumnName: nameof(HourlyPriceTrainer.Area)))
                .Append(_mlContext.Transforms.Concatenate("Features", nameof(HourlyPriceTrainer.Date), "Area"));

        var trainer = _mlContext.Regression.Trainers.Sdca(labelColumnName: "Label", featureColumnName: "Features",maximumNumberOfIterations:100);

        var trainingPipeline = dataProcessPipeline.Append(trainer);

        _model = trainingPipeline.Fit(dataView);

        IDataView predictions = _model.Transform(dataView);
        var metrics = _mlContext.Regression.Evaluate(predictions, labelColumnName: "Label", scoreColumnName: "Score");

        Console.WriteLine(metrics.LossFunction);
    }

    public void RetrainModel(object? state)
    {
        Console.WriteLine("Retraining model...");

        var data = LoadDataFromDb();
        
        TrainModel(data);

        _timer = new Timer(RetrainModel);
        _timer.Change(1209600,Timeout.Infinite);

        Console.WriteLine("Model retrained! Next retraining scheduled in 2 weeks");
    }

    public List<HourlyPrice> PredictDate(DateOnly date, string area)
    {
        var engine = _mlContext.Model.CreatePredictionEngine<HourlyPriceTrainer, HourlyPricePrediction>(_model);
        List<HourlyPrice> data = new List<HourlyPrice>(24);
        for (int i = 0; i < 24; i++)
        {
            var hour = i.ToString();
            hour = hour.Length > 1 ? hour : "0" + hour;
            TimeOnly time = TimeOnly.Parse($"{hour}:00");
            HourlyPriceTrainer input = new HourlyPriceTrainer(GetUnixTimestamp(date,time),area);
            HourlyPricePrediction predicted = new HourlyPricePrediction();
            engine.Predict(input,ref predicted);
            var predData = new HourlyPrice(time, date, predicted.Price, area)
            {
                Predicted = true
            };
            data.Add(predData);
        }
        return data;
    }
}

public class HourlyPricePrediction
{
    [ColumnName("Score")] 
    public float Price;
}

public class HourlyPriceTrainer
{
    public float Date { get; set; }
    
    public float Price { get; set; }
    public string Area { get; set; }

    public HourlyPriceTrainer(float date, float price, string area)
    {
        Date = date;
        Price = price;
        Area = area;
    }

    public HourlyPriceTrainer(float date, string area)
    {
        Date = date;
        Area = area;
    }
}