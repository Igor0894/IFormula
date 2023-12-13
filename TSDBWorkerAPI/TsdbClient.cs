using System.Net;
using TSDBWorkerAPI.Models;
using Newtonsoft.Json;
using RestSharp;
using Microsoft.Extensions.Logging;
using static TSDBWorkerAPI.Models.SubscriptionTagResultsResponse;
using Newtonsoft.Json.Linq;

namespace TSDBWorkerAPI
{
    public class TsdbClient: IDisposable
#nullable disable
    {
        public ILogger<TsdbClient> logger;
        private RestClient client;
        private int timeout = 10000;
        private bool useTimeout = false;
        string _TSDBServerURI = "";
        string _Login = "";
        string _Pass = "";
        public bool IsConnected = false;
        private LoginResStruct loginData;
        private DateTime lastTokenUpdate = DateTime.MinValue;
        public TsdbClient(TsdbSettings tsdbSettings)
        {
            _TSDBServerURI = tsdbSettings.TSDBAddress;
            _Login = tsdbSettings.TSDBLogin;
            _Pass = tsdbSettings.TSDBPassword;
            client = new RestClient();
        }
        public async Task UpdateSession() //обновляем ключ сессии 
        {
            try
            {
                loginData = await LogIn(_Login, _Pass);
                lastTokenUpdate = DateTime.Now;
                logger.LogInformation($"Обновлена сессия авторизации TSDB WebApi, истекает через {loginData.expires_in} секунд");
            }
            catch(Exception ex)
            {
                logger.LogError($"Неуспешная попытка авторизации TSDB: {ex.Message}");
                throw new Exception($"Неуспешная попытка авторизации TSDB: {ex.Message}");
            }
        }
        private async Task<LoginResStruct> LogIn(string Login, string Pass) //логин в ТСДБ
        {
            RestRequest request = new RestRequest(_TSDBServerURI + "/Auth/Token", Method.Post);
            request.Method = Method.Post;
            if (useTimeout) { request.Timeout = timeout; }
            request.AddHeader("Content-Type", "application/x-www-form-urlencoded");
            request.AddParameter("grant_type", "password");
            request.AddParameter("username", Login);
            request.AddParameter("password", Pass);
            RestResponse response = await client.ExecuteAsync(request);
            if (response.StatusCode == HttpStatusCode.OK)
            {
                IsConnected = true;
                return JsonConvert.DeserializeObject<LoginResStruct>(response.Content);
            }
            else
            {
                IsConnected = false;
                throw new Exception($"{response.StatusCode} {response.Content}");
            }
        }
        public async Task WriteDoubleVals(IDictionary<string, List<TSDBSimpleValue>> ValuesForWrite)
        {
            //метод делит на части значения для записи по принципу - до 100 тегов в одной части
            int size = 100;
            var Parts = ValuesForWrite.Select((s, i) => ValuesForWrite.Skip(i * size).Take(size).ToList()).Where(a => a.Any()).ToList(); //делим на куски по 100 максимум
            await Parallel.ForEachAsync(Parts, async (part, token) =>
            {
                await WriteDoubleValsIntoTSDBViaAPI(part.ToDictionary(x => x.Key, x => x.Value));
            });
        }
        public async Task WriteLongVals(IDictionary<string, List<TSDBSimpleValue>> ValuesForWrite)
        {
            //метод делит на части значения для записи по принципу - до 100 тегов в одной части
            int size = 100;
            var Parts = ValuesForWrite.Select((s, i) => ValuesForWrite.Skip(i * size).Take(size).ToList()).Where(a => a.Any()).ToList(); //делим на куски по 100 максимум
            await Parallel.ForEachAsync(Parts, async (part, token) =>
            {
                await WriteLongValsIntoTSDBViaAPI(part.ToDictionary(x => x.Key, x => x.Value));
            });
        }
        public async Task WriteStringVals(IDictionary<string, List<TSDBSimpleValue>> ValuesForWrite)
        {
            //метод делит на части значения для записи по принципу - до 100 тегов в одной части
            int size = 100;
            var Parts = ValuesForWrite.Select((s, i) => ValuesForWrite.Skip(i * size).Take(size).ToList()).Where(a => a.Any()).ToList(); //делим на куски по 100 максимум
            await Parallel.ForEachAsync(Parts, async (part, token) =>
            {
                await WriteStringValsIntoTSDBViaAPI(part.ToDictionary(x => x.Key, x => x.Value));
            });
        }
        public async Task WriteFloatVals(IDictionary<string, List<TSDBSimpleValue>> ValuesForWrite)
        {
            //метод делит на части значения для записи по принципу - до 100 тегов в одной части
            int size = 100;
            var Parts = ValuesForWrite.Select((s, i) => ValuesForWrite.Skip(i * size).Take(size).ToList()).Where(a => a.Any()).ToList(); //делим на куски по 100 максимум
            foreach (var part in Parts)
            {
                await WriteFloatValsIntoTSDBViaAPI(part.ToDictionary(x => x.Key, x => x.Value));
            }
        }
        public async Task WriteDoubleValsIntoTSDBViaAPI(IDictionary<string, List<TSDBSimpleValue>> ValuesForWrite)// Массовая запись значений в теги ТСДБ
        {
            RestRequest request = new RestRequest(_TSDBServerURI + "/Data", Method.Post);
            TagValuesForWriteDbl TFW = new TagValuesForWriteDbl(
                ValuesForWrite.Select(x => new TagForReqDbl(
                                                    x.Key,
                                                    x.Value.Select(y => new DblDatapoint(y)).ToArray())).ToArray());
            var body = JsonConvert.SerializeObject(TFW);
            RestResponse response = await ExecuteRequest(request, body);
            if (response.StatusCode == HttpStatusCode.OK)
            {
                //успешно
            }
            else
            {
                throw new Exception($"При записи данных возникла проблема!\nСтатус:{response.StatusCode}\nЗапрос\n{body}\nполученный ответ сервера:\n{response.Content}");
            }
        }
        public async Task WriteLongValsIntoTSDBViaAPI(IDictionary<string, List<TSDBSimpleValue>> ValuesForWrite)// Массовая запись значений в теги ТСДБ
        {
            RestRequest request = new RestRequest(_TSDBServerURI + "/Data", Method.Post);
            TagValuesForWriteLong TFW = new TagValuesForWriteLong(
                ValuesForWrite.Select(x => new TagForReqLong(
                                                    x.Key,
                                                    x.Value.Select(y => new LongDatapoint(y)).ToArray())).ToArray());
            var body = JsonConvert.SerializeObject(TFW);
            RestResponse response = await ExecuteRequest(request, body);
            if (response.StatusCode == HttpStatusCode.OK)
            {
                //успешно
            }
            else
            {
                throw new Exception($"При записи данных возникла проблема!\nСтатус:{response.StatusCode}\nЗапрос\n{body}\nполученный ответ сервера:\n{response.Content}");
            }
        }
        public async Task WriteStringValsIntoTSDBViaAPI(IDictionary<string, List<TSDBSimpleValue>> ValuesForWrite)// Массовая запись значений в теги ТСДБ
        {
            RestRequest request = new RestRequest(_TSDBServerURI + "/Data", Method.Post);
            TagValuesForWriteString TFW = new TagValuesForWriteString(
                ValuesForWrite.Select(x => new TagForReqStr(
                                                    x.Key,
                                                    x.Value.Select(y => new StringDatapoint(y)).ToArray())).ToArray());
            var body = JsonConvert.SerializeObject(TFW);
            RestResponse response = await ExecuteRequest(request, body);
            if (response.StatusCode == HttpStatusCode.OK)
            {
                //успешно
            }
            else
            {
                throw new Exception($"При записи данных возникла проблема!\nСтатус:{response.StatusCode}\nЗапрос\n{body}\nполученный ответ сервера:\n{response.Content}");
            }
        }
        public async Task WriteFloatValsIntoTSDBViaAPI(IDictionary<string, List<TSDBSimpleValue>> ValuesForWrite)// Массовая запись значений в теги ТСДБ
        {
            RestRequest request = new RestRequest(_TSDBServerURI + "/Data", Method.Post);

            TagValuesForWriteFloat TFW = new TagValuesForWriteFloat(
                ValuesForWrite.Select(x => new TagForReqFlt(
                                                    x.Key,
                                                    x.Value.Select(y => new FloatDatapoint(y)).ToArray())).ToArray());
            var body = JsonConvert.SerializeObject(TFW);
            RestResponse response = await ExecuteRequest(request, body);
            if (response.StatusCode == HttpStatusCode.OK)
            {
                //успешно
            }
            else
            {
                throw new Exception($"При записи данных возникла проблема!\nСтатус:{response.StatusCode}\nЗапрос\n{body}\nполученный ответ сервера:\n{response.Content}");
            }
        }
        public async Task<bool> GetTagViaAPI(string Tag)// Проверка, есть ли такой тег
        {
            RestRequest request = new RestRequest(_TSDBServerURI + "/Tag/GetTag", Method.Post);
            var body = @"{" + "\n" +
                        @"    ""TagName"": """ + Tag + @"""" + "\n" +
                        @"}";
            RestResponse response = await ExecuteRequest(request, body);
            if (response.StatusCode == HttpStatusCode.OK)
            {
                if (response.Content != "null")
                    return true;
            }
            return false;
        }
        public async Task<Dictionary<string, List<TSDBValue>>> GetArcValue<T>(string tagName, DateTime timestamp, RetrievalTypeConstants boundaryType)
        {
            RestRequest request = new RestRequest(_TSDBServerURI + "/Data/ArcValue", Method.Post);
            string body = $"{{\"Request\":{{\"Data\":{{\"{tagName}\":[\"{timestamp.ToUniversalTime().ToString("yyyy-MM-ddTHH:mm:ssZ")}\"]}},\"Mode\":{(int)boundaryType}}}}}";
            RestResponse response = await ExecuteRequest(request, body);
            if (response.StatusCode == HttpStatusCode.OK)
            {
                DataArcValuesResponse arcValueResp = JsonConvert.DeserializeObject<DataArcValuesResponse>(response.Content);
                Dictionary<string, List<TSDBValue>> TagValues = new Dictionary<string, List<TSDBValue>>();
                foreach (var Tag in arcValueResp.tags)
                {
                    string responseTagName = Tag.name;
                    List<TSDBValue> vals = new List<TSDBValue>();
                    if (Tag.dataPoints is null) { continue; }
                    foreach (var dataPoint in Tag.dataPoints)
                    {
                        TSDBValue V = GetTsdbValue<T>(dataPoint, responseTagName);
                        vals.Add(V);
                    }
                    TagValues.Add(responseTagName, vals);
                }
                return TagValues;
            }
            else
                throw new Exception($"Ошибка: {response.ErrorMessage}{response.ErrorException} \rв запросе: {body}");

        }
        public async Task<Dictionary<string, List<TSDBValue>>> Data_RecordedValuesByCount(IList<string> tagNames, DateTime startTime, int requestedCount, Direction direction, BoundaryType boundaryType, FilterType filterType = FilterType.All)
        {
            RestRequest request = new RestRequest(_TSDBServerURI + "/Data/RecordedValuesByCount", Method.Post);
            Data_RecordedValuesByCount_REQ r = new Data_RecordedValuesByCount_REQ(
                tagNames,
                startTime,
                requestedCount,
                direction,
                boundaryType,
                filterType);
            string body = JsonConvert.SerializeObject(r);
            RestResponse response = await ExecuteRequest(request, body);
            if (response.StatusCode == HttpStatusCode.OK)
            {
                DataArcValuesResponse Resp = JsonConvert.DeserializeObject<DataArcValuesResponse>(response.Content);
                Dictionary<string, List<TSDBValue>> TagValues = new Dictionary<string, List<TSDBValue>>();
                foreach (var Tag in Resp.tags)
                {
                    string TagName = Tag.name;
                    List<TSDBValue> vals = new List<TSDBValue>();
                    if (Tag.dataPoints is null) { continue; }
                    foreach (var dataPoint in Tag.dataPoints)
                    {
                        string annotation = dataPoint.annotation is null ? "" : dataPoint.annotation;
                        Quality quality = dataPoint.qualityMark is null ? Quality.good : (Quality)dataPoint.qualityMark.stateNumber;
                        TSDBValue V = new TSDBValue(TagName, dataPoint.timeStamp, dataPoint.valueDouble, annotation, quality);
                        vals.Add(V);
                    }
                    TagValues.Add(TagName, vals);
                }
                return TagValues;
            }
            else
                throw new Exception(response.ErrorMessage);

        }
        public async Task<Dictionary<string, List<TSDBValue>>> Data_RecordedValuesByCount_String(IList<string> tagNames, DateTime startTime, int requestedCount, Direction direction, BoundaryType boundaryType, FilterType filterType = FilterType.All)
        {
            RestRequest request = new RestRequest(_TSDBServerURI + "/Data/RecordedValuesByCount", Method.Post);
            Data_RecordedValuesByCount_REQ r = new Data_RecordedValuesByCount_REQ(
                tagNames,
                startTime,
                requestedCount,
                direction,
                boundaryType,
                filterType);

            string body = JsonConvert.SerializeObject(r);
            RestResponse response = await ExecuteRequest(request, body);
            if (response.StatusCode == HttpStatusCode.OK)
            {
                Data_RecordedValuesByCount_RESPONSE_String Resp = JsonConvert.DeserializeObject<Data_RecordedValuesByCount_RESPONSE_String>(response.Content);
                Dictionary<string, List<TSDBValue>> TagValues = new Dictionary<string, List<TSDBValue>>();
                foreach (var Tag in Resp.tags)
                {
                    string TagName = Tag.name;
                    List<TSDBValue> vals = new List<TSDBValue>();
                    foreach (var dataPoint in Tag.dataPoints)
                    {
                        TSDBValue V = new TSDBValue(TagName, dataPoint.timeStamp, dataPoint.valueString, "", (Quality)dataPoint.qualityMark.stateNumber);
                        vals.Add(V);
                    }
                    TagValues.Add(TagName, vals);
                }
                return TagValues;
            }
            else
                return null;

        }
        public async Task DeleteAllDataFromTag(string Tag)
        {
            RestRequest request = new RestRequest(_TSDBServerURI + "/Data/DeleteDataPoints", Method.Delete);
            var body =
            @"{""Request"":{ " + "\n" +
            @"  ""Tag"":""" + Tag + @"""," + "\n" +
            @"  ""Limit"": 10," + "\n" +
            @"  ""StartDateUTC"": """ + new DateTime(1970, 01, 01).ToString("yyyy-MM-ddTHH:mm:ss.fffZ") + @"""," + "\n" +
            @"  ""EndDateUTC"": """ + new DateTime(2099, 12, 31).ToString("yyyy-MM-ddTHH:mm:ss.fffZ") + @"""" + "\n" +
            @"}}";
            RestResponse response = await ExecuteRequest(request, body);
            if (response.StatusCode == HttpStatusCode.NoContent) //NoContent - нормальный резулттат для удаления
            {
                //успешно
            }
            else
            {
                throw new Exception($"При удалении данных в теге [{Tag}] возникла проблема! Возможно тег отсутствует!]\nСтатус: {response.StatusCode.ToString()}\nОтвет сервера:{response.Content}");
            }
        }
        public async Task MakeTag(string TagName, string Type)
        {
            RestRequest request = new RestRequest(_TSDBServerURI + "/Tag", Method.Post);
            var body = "{\r\n  \"Request\": {\r\n    \"Name\": \"" + TagName + "\",\r\n    \"Type\": \"" + Type + "\"\r\n  }\r\n}";
            RestResponse response = await ExecuteRequest(request, body);
            if (response.StatusCode == HttpStatusCode.OK)
            {

            }
        }
        public async Task<List<TSDBValue>> GetTakeFrameByTag<T>(string tagName, DateTime StartTime, DateTime EndtTime)
        {
            RestRequest request = new RestRequest(_TSDBServerURI + "/Data/GetByList", Method.Post);
            string body = "{\"SearchParamses\":[ {\r\n  \"Tag\": \"" + tagName + "\",\r\n  \"StartDateUTC\": \"" + StartTime.ToUniversalTime().ToString("yyyy-MM-ddTHH:mm:ss") + "\",\r\n  \"EndDateUTC\": \"" + EndtTime.ToUniversalTime().ToString("yyyy-MM-ddTHH:mm:ss") + "\"\r\n  } ]}";
            RestResponse response = await ExecuteRequest(request, body);
            if (response.StatusCode == HttpStatusCode.OK)
            {
                DataArcValuesResponse Resp = JsonConvert.DeserializeObject<DataArcValuesResponse>(response.Content);
                List<TSDBValue> vals = new List<TSDBValue>();
                foreach (var dataPoint in Resp.tags[0].dataPoints)
                {
                    TSDBValue V = GetTsdbValue<T>(dataPoint, tagName);
                    vals.Add(V);
                }
                return vals;
            }
            else
                return null;
        }
        public async Task<string> GetMetaAttribute(string Tag, string AttributeCode)
        {
            RestRequest request = new RestRequest(_TSDBServerURI + "/MetaData/GetMetaAttribute", Method.Post);
            var body = "{ \"Request\":{ \"id\":\"" + Tag + "\",\"attributeCode\":\"" + AttributeCode + "\" } }";
            RestResponse response = await ExecuteRequest(request, body);
            if (response.StatusCode == HttpStatusCode.OK)
            {
                //успешно
                GetAtributeFromTag AttrInfo = JsonConvert.DeserializeObject<GetAtributeFromTag>(response.Content);
                return AttrInfo.value;
            }
            else
            {
                throw new Exception($"При попытке получения атрибута [{AttributeCode}] для тега [{Tag}] возникла проблема!\nСтатус: {response.StatusCode.ToString()}");
            }
        }
        public async Task<string> GetDescription(string Tag)
        {
            return await GetMetaAttribute(Tag, "Description");
        }
        public async Task<TSDBValue> Summary(string tagName, DateTime startTime, DateTime endTime, SummaryType summaryType, CalculationBasis calculationBasis)
        {
            RestRequest request = new RestRequest(_TSDBServerURI + "/Data/GetSummary", Method.Post);
            SummaryRequest r = new SummaryRequest(
                tagName,
                startTime,
                endTime,
                1,
                summaryType,
                calculationBasis);

            string body = JsonConvert.SerializeObject(r);
            RestResponse response = await ExecuteRequest(request, body);
            if (response.StatusCode == HttpStatusCode.OK)
            {
                SummaryTotalResponse[] Resp = JsonConvert.DeserializeObject<SummaryTotalResponse[]>(response.Content);
                TSDBValue V = GetSummaryTsdbValue(Resp[0], tagName, summaryType);
                return V;
            }
            else
                throw new Exception($"Ошибка: {response.ErrorMessage}{response.ErrorException} \rв чтении ответа: {response.Content.Replace("\"", "")} на запрос Summary: {body}");
        }
        public async Task<Guid> GetFirstSubscriptionGuid(string[] tags, TimeSpan timeToLive, int maxCountPointsInBuffer)
        {
            RestRequest request = new RestRequest(_TSDBServerURI + "/Subscription", Method.Post);
            List<SubscriptionRequest.Tag> tagsForRequest = new List<SubscriptionRequest.Tag> { };
            foreach(var tag in tags)
            {
                SubscriptionRequest.Tag tagForRequest = new SubscriptionRequest.Tag(tag);
                tagsForRequest.Add(tagForRequest);
            }
            SubscriptionRequest r = new SubscriptionRequest(
                tagsForRequest.ToArray(),
                timeToLive,
                maxCountPointsInBuffer);

            string body = JsonConvert.SerializeObject(r);
            RestResponse response = await ExecuteRequest(request, body);
            if (response.StatusCode == HttpStatusCode.OK)
            {
                if(Guid.TryParse(response.Content.Replace("\"", ""), out Guid subscriptionGuid))
                {
                    return subscriptionGuid;
                }
                else
                {
                    throw new Exception($"Ошибка: {response.ErrorMessage}{response.ErrorException} \rв чтении ответа: {response.Content.Replace("\"","")} на запрос подписки: {body}");
                }
            }
            else
                throw new Exception($"Ошибка: {response.ErrorMessage}{response.ErrorException} \rв запросе подписки: {body}");

        }
        public async Task<Guid> GetSubscriptionGuid(string[] tags, TimeSpan timeToLive, int maxCountPointsInBuffer, DateTime archiveDataStartTimestamp)
        {
            RestRequest request = new RestRequest(_TSDBServerURI + "/Subscription", Method.Post);
            List<SubscriptionRequest.Tag> tagsForRequest = new List<SubscriptionRequest.Tag> { };
            foreach (var tag in tags)
            {
                SubscriptionRequest.Tag tagForRequest = new SubscriptionRequest.Tag(tag, archiveDataStartTimestamp);
                tagsForRequest.Add(tagForRequest);
            }
            SubscriptionRequest r = new SubscriptionRequest(
                tagsForRequest.ToArray(),
                timeToLive,
                maxCountPointsInBuffer);

            string body = JsonConvert.SerializeObject(r);
            RestResponse response = await ExecuteRequest(request, body);
            if (response.StatusCode == HttpStatusCode.OK)
            {
                if (Guid.TryParse(response.Content.Replace("\"", ""), out Guid subscriptionGuid))
                {
                    return subscriptionGuid;
                }
                else
                {
                    throw new Exception($"Ошибка: {response.ErrorMessage}{response.ErrorException} \rв чтении ответа: {response.Content.Replace("\"", "")} на запрос подписки: {body}");
                }
            }
            else
                throw new Exception($"Ошибка: {response.ErrorMessage}{response.ErrorException} \rв запросе подписки: {body}");

        }
        public async Task<Dictionary<string, List<TSDBValue>>> GetSubscriptionData(Guid SubscriptionGuid, int pointsLimit)
        {
            RestRequest request = new RestRequest(_TSDBServerURI + $"/Subscription/{SubscriptionGuid}/Data", Method.Get);
            string body = $"{{\"limit\":{pointsLimit}}}";
            RestResponse response = await ExecuteRequest(request, body);
            if (response.StatusCode == HttpStatusCode.OK)
            {
                SubscriptionTagResultsResponse Resp = JsonConvert.DeserializeObject<SubscriptionTagResultsResponse>(response.Content);
                Dictionary<string, List<TSDBValue>> TagValues = new Dictionary<string, List<TSDBValue>>();
                foreach (var result in Resp.subscriptionTagResults)
                {
                    Tag tag = result.tag;
                    string TagName = tag.name;
                    List<TSDBValue> vals = new List<TSDBValue>();
                    if (tag.dataPoints is null) { continue; }
                    foreach (var dataPoint in tag.dataPoints)
                    {
                        string annotation = dataPoint.annotation is null ? "" : dataPoint.annotation;
                        Quality quality = dataPoint.qualityMark is null ? Quality.good : (Quality)dataPoint.qualityMark.stateNumber;
                        TSDBValue V = new TSDBValue(TagName, dataPoint.timeStamp, dataPoint.valueDouble, annotation, quality);
                        vals.Add(V);
                    }
                    TagValues.Add(TagName, vals);
                }
                return TagValues;
            }
            else
                throw new Exception($"Ошибка: {response.ErrorMessage}{response.ErrorException} \rв запросе данных по подписке: {body} {SubscriptionGuid}");

        }
        public async Task<bool> CheckSubscription(Guid SubscriptionGuid)
        {
            RestRequest request = new RestRequest(_TSDBServerURI + $"/Subscription/{SubscriptionGuid}", Method.Get);
            RestResponse response = await ExecuteRequest(request, null);
            if (response.StatusCode == HttpStatusCode.OK)
            {
                return true;
            }
            else
                return false;
        }
        public async Task UpdateSubscription(Guid SubscriptionGuid, TimeSpan timeToLive)
        {
            RestRequest request = new RestRequest(_TSDBServerURI + $"/Subscription/{SubscriptionGuid}", Method.Put);
            string body = $"{{\"NewTimeToLive\":\"{timeToLive}\"}}";
            RestResponse response = await ExecuteRequest(request, body);
            if (response.StatusCode == HttpStatusCode.OK)
            {
            }
            else
            {
                logger.LogError($"Ошибка: {response.ErrorMessage}{response.ErrorException} \rв обновлении подписки, запрос: {body}\rответ: {response.Content}");
                throw new Exception($"Ошибка: {response.ErrorMessage}{response.ErrorException} \rв обновлении подписки: {body}");
            }
        }
        public void Dispose()
        {
            client?.Dispose();
            GC.SuppressFinalize(this);
        }
        public async Task<TSDBValue> GetSnapshotByTag<T>(string Tag)
        {
            RestRequest request = new RestRequest(_TSDBServerURI + "/Snapshot/GetSnapshotByTag", Method.Post);
            var body = "{ \"tagName\":\"" + Tag + "\" }";
            RestResponse response = await ExecuteRequest(request, body);
            if (response.StatusCode == HttpStatusCode.OK)
            {
                SnapshotResponse SnapInfo = JsonConvert.DeserializeObject<SnapshotResponse>(response.Content);
                TSDBValue V = GetTsdbValue<T>(SnapInfo.dataPoint, SnapInfo.tagName);
                return V;
            }
            else
            {
                throw new Exception($"При попытке получения Snapshot для тега [{Tag}] возникла проблема!\nСтатус: {response.StatusCode.ToString()}");
            }
        }
        public async Task<TagInfoResponse> GetTagInfo(string Tag)
        {
            RestRequest request = new RestRequest(_TSDBServerURI + "/Tag/GetTag", Method.Post);
            var body = "{ \"tagName\":\"" + Tag + "\" }";
            RestResponse response = await ExecuteRequest(request, body);
            if (response.StatusCode == HttpStatusCode.OK)
            {
                TagInfoResponse TagInfo = JsonConvert.DeserializeObject<TagInfoResponse>(response.Content);
                return TagInfo;
            }
            else
            {
                throw new Exception($"При попытке получения информацию о теге [{Tag}] возникла проблема!\nСтатус: {response.StatusCode}");
            }
        }
        public async Task<string[]> GetTagsByPointSource(string pointSource)
        {
            RestRequest request = new RestRequest(_TSDBServerURI + "/Admin/SearchTags", Method.Post);
            var body = "[{ \"Code\":\"PointSource\", \"ValueSearchTmpl\":\"" + pointSource + "\" }]";
            RestResponse response = await ExecuteRequest(request, body);
            if (response.StatusCode == HttpStatusCode.OK)
            {
                string[] TagInfo = JsonConvert.DeserializeObject<string[]>(response.Content);
                return TagInfo;
            }
            else
            {
                throw new Exception($"При попытке получения списка тегов с PointSource [{pointSource}] возникла проблема!\nСтатус: {response.StatusCode}");
            }
        }
        private TSDBValue GetTsdbValue<T>(Datapoint apiValue, string tagName)
        {
            string annotation = apiValue.annotation is null ? "" : apiValue.annotation;
            Quality quality = apiValue.qualityMark is null ? Quality.good : (Quality)apiValue.qualityMark.stateNumber;
            if (typeof(T) == typeof(double))
            {
                return new TSDBValue(tagName, apiValue.timeStamp, apiValue.valueDouble, annotation, quality);
            }
            else if (typeof(T) == typeof(long))
            {
                return new TSDBValue(tagName, apiValue.timeStamp, apiValue.valueLong, annotation, quality);
            }
            else if (typeof(T) == typeof(float))
            {
                return new TSDBValue(tagName, apiValue.timeStamp, apiValue.valueFloat, annotation, quality);
            }
            else
            {
                return new TSDBValue(tagName, apiValue.timeStamp, apiValue.valueString, annotation, quality);
            }
        }
        private TSDBValue GetSummaryTsdbValue(SummaryTotalResponse apiValue, string tagName, SummaryType summaryType)
        {
            switch (summaryType)
            {
                case SummaryType.Total:
                    return new TSDBValue(tagName, apiValue.endIntervalUTC, apiValue.summaryDatа.total.ToString(), "", Quality.good);
                case SummaryType.Average:
                    return new TSDBValue(tagName, apiValue.endIntervalUTC, apiValue.summaryDatа.average.ToString(), "", Quality.good);
                case SummaryType.Maximum:
                    return new TSDBValue(tagName, apiValue.endIntervalUTC, apiValue.summaryDatа.doubleMax.ToString(), "", Quality.good);
                case SummaryType.Minimum:
                    return new TSDBValue(tagName, apiValue.endIntervalUTC, apiValue.summaryDatа.doubleMin.ToString(), "", Quality.good);
                case SummaryType.Count:
                    return new TSDBValue(tagName, apiValue.endIntervalUTC, apiValue.summaryDatа.count.ToString(), "", Quality.good);
                case SummaryType.All:
                    return new TSDBValue(tagName, apiValue.endIntervalUTC, apiValue.summaryDatа.total.ToString(), "", Quality.good);
                case SummaryType.StandardDeviation:
                    return new TSDBValue(tagName, apiValue.endIntervalUTC, apiValue.summaryDatа.standardDeviation.ToString(), "", Quality.good);
                case SummaryType.Range:
                    return new TSDBValue(tagName, apiValue.endIntervalUTC, apiValue.summaryDatа.doubleRange.ToString(), "", Quality.good);
                case SummaryType.PercentGood:
                    return new TSDBValue(tagName, apiValue.endIntervalUTC, apiValue.summaryDatа.percentGood.ToString(), "", Quality.good);
                default:
                    return null;
            }
        }
        private async Task<RestResponse> ExecuteRequest(RestRequest request, string body)
        {
            if (lastTokenUpdate == DateTime.MinValue || DateTime.Now - lastTokenUpdate > new TimeSpan(0, 0, loginData.expires_in - 3600)) { await UpdateSession(); }
            request.AddHeader("Content-Type", "application/json");
            request.AddHeader("Authorization", string.Format("Bearer {0}", loginData.access_token));
            if (!string.IsNullOrEmpty(body)) { request.AddParameter("application/json", body, ParameterType.RequestBody); }
            try
            {
                if (useTimeout) { request.Timeout = timeout; }
                RestResponse response = await client.ExecuteAsync(request);
                logger.LogDebug($"\rRequest:{request.Resource}\r{request.Parameters.FirstOrDefault().Value}\rResponse:{response.Content}");
                return response;
            }
            catch(Exception ex)
            {
                logger.LogError($"Ошибка при выполнении запроса {request.Resource} {body}: \r{ex.Message}\r{ex.InnerException}");
                throw new Exception($"Не удалось выполнить запрос {request.Resource} {body}: \r{ex.Message}\r{ex.InnerException}");
            }
        }
    }
    #region Вспомогательные классы чтобы отвязатья от TSDB.SDK
    public class TSDBSimpleValue
    {
        public object Value
        {
            get;
            set;
        }

        public DateTime? TimestampUTC
        {
            get;
            set;
        }

        public string Annotation
        {
            get;
            set;
        } = null;


        public Quality Quality
        {
            get;
            set;
        } = Quality.good;
        public override string ToString() => string.Format("{0} {1} {2} {3} ", (object)this.TimestampUTC, this.Value, (object)this.Quality, (object)this.Annotation);
    }
    public enum Value_Type
    {
        DOUBLE,
        LONG,
        STRING,
        SET,
        FLOAT,
        DATETIME
    }
    public enum Quality : ushort
    {
        bad = 0,
        badConfigurationError = 4,
        badNotConnected = 8,
        badDeviceFailure = 12,
        badSensorFailure = 0x10,
        badLastKnownValue = 20,
        badCommFailure = 24,
        badOutOfService = 28,
        badWaitingForInitialData = 0x20,
        uncertain = 0x40,
        uncertainLastUsableValue = 68,
        uncertainSensorNotAccurate = 80,
        uncertainEUExceeded = 84,
        uncertainSubNormal = 88,
        good = 192,
        goodLocalOverride = 216
    }
    public class GetAtributeFromTag
    {
        public string code { get; set; }
        public string value { get; set; }
        public string description { get; set; }
        public string viewName { get; set; }
        public DateTime createDateUTC { get; set; }
        public int createUserId { get; set; }
    }
    #endregion
    #region Вспомогательные классы для сериализации / десериализации JSON
    class LoginResStruct
    {
        public string access_token = "";
        public string token_type = "";
        public int expires_in = 0;
    }
    class TagValuesForWriteDbl
    {
        public TagForReqDbl[] Tags;
        public TagValuesForWriteDbl(TagForReqDbl[] Tags)
        {
            this.Tags = Tags;
        }
    }
    class TagValuesForWriteLong
    {
        public TagForReqLong[] Tags;
        public TagValuesForWriteLong(TagForReqLong[] Tags)
        {
            this.Tags = Tags;
        }
    }
    class TagValuesForWriteString
    {
        public TagForReqStr[] Tags;
        public TagValuesForWriteString(TagForReqStr[] Tags)
        {
            this.Tags = Tags;
        }
    }
    class TagValuesForWriteFloat
    {
        public TagForReqFlt[] Tags;
        public TagValuesForWriteFloat(TagForReqFlt[] Tags)
        {
            this.Tags = Tags;
        }
    }
    class TagForReqDbl
    {
        public string Name;
        public DblDatapoint[] DataPoints;
        public TagForReqDbl(string Name, DblDatapoint[] DataPoints)
        {
            this.Name = Name;
            this.DataPoints = DataPoints;
        }
    }
    class TagForReqStr
    {
        public string Name;
        public StringDatapoint[] DataPoints;
        public TagForReqStr(string Name, StringDatapoint[] DataPoints)
        {
            this.Name = Name;
            this.DataPoints = DataPoints;
        }
    }
    class TagForReqFlt
    {
        public string Name;
        public FloatDatapoint[] DataPoints;
        public TagForReqFlt(string Name, FloatDatapoint[] DataPoints)
        {
            this.Name = Name;
            this.DataPoints = DataPoints;
        }
    }
    class DblDatapoint
    {
        public object ValueDouble;

        public DateTime? TimeStamp;
        public Qualitymark QualityMark;
        public DblDatapoint(TSDBSimpleValue V)
        {
            this.ValueDouble = V.Value;
            this.TimeStamp = V.TimestampUTC;
            this.QualityMark = new Qualitymark(V.Quality);
        }

    }
    public class Qualitymark
    {
        public string Value = "good";
        public Qualitymark()
        {

        }
        public Qualitymark(Quality Quality)
        {
            switch (Quality)
            {
                case Quality.good:
                    Value = "good";
                    break;
                case Quality.goodLocalOverride:
                    Value = "good";
                    break;
                default:
                    Value = "bad";
                    break;
            }
        }
    }
    class TagForReqLong
    {
        public string Name;
        public LongDatapoint[] DataPoints;
        public TagForReqLong(string Name, LongDatapoint[] DataPoints)
        {
            this.Name = Name;
            this.DataPoints = DataPoints;
        }
    }
    class LongDatapoint
    {
        public object ValueLong;

        public DateTime? TimeStamp;
        public Qualitymark QualityMark;
        public LongDatapoint(TSDBSimpleValue V)
        {
            this.ValueLong = V.Value;
            this.TimeStamp = V.TimestampUTC;
            this.QualityMark = new Qualitymark(V.Quality);
        }

    }
    class StringDatapoint
    {
        public object ValueString;

        public DateTime? TimeStamp;
        public Qualitymark QualityMark;
        public StringDatapoint(TSDBSimpleValue V)
        {
            this.ValueString = V.Value;
            this.TimeStamp = V.TimestampUTC;
            this.QualityMark = new Qualitymark(V.Quality);
        }
    }
    class FloatDatapoint
    {
        public object ValueFloat;

        public DateTime? TimeStamp;
        public Qualitymark QualityMark;
        public FloatDatapoint(TSDBSimpleValue V)
        {
            this.ValueFloat = V.Value;
            this.TimeStamp = V.TimestampUTC;
            this.QualityMark = new Qualitymark(V.Quality);
        }
    }
    #endregion
}
