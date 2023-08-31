using Microsoft.ML;
using Microsoft.ML.AutoML;
using Microsoft.ML.Data;
using StromAPI.Models;
using static Microsoft.ML.DataOperationsCatalog;

namespace StromAPI;

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

    public static float GetUnixTimestamp(DateOnly date, DateOnly earliest)
    {
        DateTime combinedDateTime = date.ToDateTime(new TimeOnly(0, 0));

        DateTime earliestDateTime = new DateTime(earliest.Year, earliest.Month, earliest.Day, 0, 0, 0, DateTimeKind.Utc);
        earliestDateTime = earliestDateTime.ToLocalTime();

        TimeSpan timeSpan = combinedDateTime - earliestDateTime;
        return (float)timeSpan.TotalSeconds;
    }


    private HourlyPriceTrainer[] LoadDataFromDb()
    {
        var data = _dbContext.Prices.ToArray();
        HourlyPriceTrainer[] dataOut = new HourlyPriceTrainer[data.Length];
        var earliestDate = data.Min(p => p.Date);
        var areas = new Dictionary<string, int>
        {
            {"NO1",0},
            {"NO2",1},
            {"NO3",2},
            {"NO4",3},
            {"NO5",4}
        };
        for (int i = 0;i<data.Length;i++)
        {
            dataOut[i] = new HourlyPriceTrainer(GetUnixTimestamp(data[i].Date,earliestDate), (float)data[i].Price,
                areas[data[i].Area], data[i].Time.Hour);
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
        column.CategoricalColumnNames.Add(nameof(HourlyPriceTrainer.Time));
        column.NumericColumnNames.Add(nameof(HourlyPriceTrainer.Date));

        SweepablePipeline pipeline =
            _mlContext.Auto().Featurizer(dataView, columnInformation: column)
                .Append(_mlContext.Transforms.Categorical.OneHotEncoding(inputColumnName:nameof(HourlyPriceTrainer.Area),outputColumnName:nameof(HourlyPriceTrainer.Area)))
                .Append(_mlContext.Transforms.Categorical.OneHotEncoding(inputColumnName: nameof(HourlyPriceTrainer.Time), outputColumnName: nameof(HourlyPriceTrainer.Time)))
                .Append(_mlContext.Transforms.NormalizeMinMax(nameof(HourlyPriceTrainer.Date), nameof(HourlyPriceTrainer.Date)))
                .Append(_mlContext.Auto().Regression(labelColumnName: "Label"));

        AutoMLExperiment experiment = _mlContext.Auto().CreateExperiment();

        experiment
            .SetPipeline(pipeline)
            .SetRegressionMetric(RegressionMetric.RSquared, labelColumn: "Label")
            .SetTrainingTimeInSeconds(120)
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
        List<HourlyPrice> data = new List<HourlyPrice>(24);
        HourlyPriceTrainer[] inputs = new HourlyPriceTrainer[24];
        TimeOnly[] timeArray = new TimeOnly[24];
        var earliestDate = _dbContext.Prices.Min(p => p.Date);
        var areas = new Dictionary<string, int>
        {
            {"NO1",0},
            {"NO2",1},
            {"NO3",2},
            {"NO4",3},
            {"NO5",4}
        };
        for (int i = 0; i < 24; i++)
        {
            var hour = i.ToString();
            hour = hour.Length > 1 ? hour : "0" + hour;
            TimeOnly time = TimeOnly.Parse($"{hour}:00");
            HourlyPriceTrainer input = new HourlyPriceTrainer(GetUnixTimestamp(date,earliestDate), areas[area], i);
            inputs[i] = input;
            timeArray[i] = time;
        }
        var inputData = _mlContext.Data.LoadFromEnumerable(inputs);
        foreach (var column in inputData.Schema)
        {
            Console.WriteLine($"{column.Name}: {column.Type}");
        }
        var outputs = _model.Transform(inputData);

        var current = 0;
        foreach(var price in outputs.GetColumn<float>("Score").ToArray())
        {
            data.Add(new HourlyPrice(timeArray[current], date, price, area)
            {
                Predicted = true
            });
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
    public int Time { get; set; }

    [ColumnName("Label")]
    public float Price { get; set; }
    public int Area { get; set; }

    public HourlyPriceTrainer(float date, float price, int area,int time)
    {
        Date = date;
        Time = time;
        Price = price;
        Area = area;
    }

    public HourlyPriceTrainer(float date, int area, int time)
    {
        Date = date;
        Area = area;
        Time = time;
    }

    public HourlyPriceTrainer()
    {

    }
}