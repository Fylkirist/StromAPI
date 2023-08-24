using Microsoft.ML;
using Microsoft.ML.Data;
using StrømAPI.Models;
using Microsoft.ML.AutoML;
using static Microsoft.ML.DataOperationsCatalog;

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

        TrainTestData trainValidationData = _mlContext.Data.TrainTestSplit(dataView, testFraction: 0.2);

        var column = new ColumnInformation();
        column.CategoricalColumnNames.Add(nameof(HourlyPriceTrainer.Area));
        column.NumericColumnNames.Add(nameof(HourlyPriceTrainer.Date));

        SweepablePipeline pipeline =
            _mlContext.Auto().Featurizer(dataView, columnInformation: column)
                .Append(_mlContext.Auto().Regression(labelColumnName: "Label"));

        AutoMLExperiment experiment = _mlContext.Auto().CreateExperiment();

        experiment
            .SetPipeline(pipeline)
            .SetRegressionMetric(RegressionMetric.RSquared, labelColumn: "Label")
            .SetTrainingTimeInSeconds(300)
            .SetDataset(trainValidationData);

        TrialResult result = experiment.Run();

        _model = result.Model;

        var error =_mlContext.Regression.Evaluate( _model.Transform(dataView),"Label");

        Console.WriteLine(error.LossFunction);
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
        HourlyPriceTrainer[] inputs = new HourlyPriceTrainer[24];
        TimeOnly[] timeArray = new TimeOnly[24];
        for (int i = 0; i < 24; i++)
        {
            var hour = i.ToString();
            hour = hour.Length > 1 ? hour : "0" + hour;
            TimeOnly time = TimeOnly.Parse($"{hour}:00");
            HourlyPriceTrainer input = new HourlyPriceTrainer(GetUnixTimestamp(date,time),area);
            inputs[i] = input;
            timeArray[i] = time;
        }

        var inputData = _mlContext.Data.LoadFromEnumerable(inputs);
        var outputs = _model.Transform(inputData);

        var current = 0;
        foreach(var price in outputs.GetColumn<float>("Score").ToArray())
        {
            data.Add(new HourlyPrice(timeArray[current],date,price,area));
            current++;
        }

        return data;
    }
}

public class HourlyPricePrediction
{
    [ColumnName("Score")]
    public float Price { get; set; }

    public HourlyPricePrediction()
    {

    }
}

public class HourlyPriceTrainer
{
    public float Date { get; set; }

    [ColumnName("Label")]
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

    public HourlyPriceTrainer()
    {

    }
}