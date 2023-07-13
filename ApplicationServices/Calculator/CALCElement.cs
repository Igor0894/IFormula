using ApplicationServices.Services;
using Interpreter.Delegates;
using ISP.SDK.IspObjects;
using Attribute = ISP.SDK.IspObjects.Attribute;
using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;
using System.Configuration;

namespace ApplicationServices.Calculator
{
    public class CalcElement
#nullable disable
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string Path { get; set; }
        public bool IsTriggerSchedulle { get; set; } = false;
        public Interpreter.Interpreter Interpreter = new();
        public SQL Sql = new()
        {
            ConnectionString = CalcService.ConnectionString
        };
        public BlockingCollection<CalcAttribute> Attributes = new();
        public BlockingCollection<CalcAttribute> TriggerAttributes = new();
        public List<List<CalcAttribute>> Queue = new();
        public void Initialization(Attributes children, Element ispElement, Attribute formula, ILogger<CalcService> _logger)
        {
            if (children.Contains("Триггер"))
            {
                LoadTriggersTags(ispElement, formula, _logger);
            }
            foreach (Attribute child in children)
            {
                if (child.Name == "Триггер") { continue; }
                CalcAttribute item = new()
                {
                    Name = child.Name,
                    Expression = child.Value,
                };
                Attributes writeAttributes = ispElement.Attributes.Children(child.Id);
                if (writeAttributes.Count > 0)
                {
                    if (writeAttributes.Contains("Запись"))
                    {
                        item.OutDataSource.Type = writeAttributes.Item("Запись").ValueType;
                        item.OutDataSource.Id = writeAttributes.Item("Запись").Id;
                        if (item.OutDataSource.Type == AttributeValueType.TSDB)
                            item.OutDataSource.Name = writeAttributes.Item("Запись").Value;
                        if (item.OutDataSource.Type == AttributeValueType.Static)
                            item.OutDataSource.Name = ispElement.Attributes.Item(writeAttributes.Item("Запись").Value.Replace("'", "")).Value;
                        if (item.OutDataSource.Type == AttributeValueType.SQL)
                            item.OutDataSource.Name = "SQL Атрибут";
                        item.OutDataSource.Time = writeAttributes.Contains("Время") ? writeAttributes.Item("Время").Value.Replace("'*'", "c") : "c";
                    }
                    else if (writeAttributes.Contains("Перезапись"))
                    {
                        item.OutReWriteDataSource.Type = writeAttributes.Item("Перезапись").ValueType;
                        item.OutReWriteDataSource.Id = writeAttributes.Item("Перезапись").Id;
                        if (item.OutReWriteDataSource.Type == AttributeValueType.SQL)
                        {
                            item.OutReWriteDataSource.Name = "SQL Атрибут";
                        }
                    }
                };
                Attributes.Add(item);
            }
            foreach (Attribute attribute in ispElement.Attributes) //Определение переменных из атрибутов элемента ISP
            {
                string path = "'" + attribute.Path + "'";
                string variable = string.Empty;
                foreach (CalcAttribute item in Attributes)
                {
                    if (!item.Expression.Contains(path)) continue;
                    if (string.IsNullOrEmpty(variable))
                    {
                        variable = Interpreter.GetVariableName();
                        Interpreter.SetVariable(variable, attribute.Value);
                    }
                    item.Expression = item.Expression.Replace(path, variable);
                }
            }
            Sort();
        }
        private void LoadTriggersTags(Element ispElement, Attribute formula, ILogger<CalcService> _logger)
        {
            string[] triggerAttributesName = ispElement.Attributes.Children(formula.Id).Properties.Item("Триггер").Value.Split(",");
            foreach (var triggerAttributeName in triggerAttributesName)
            {
                if (!ispElement.Attributes.Points.Contains(triggerAttributeName))
                { _logger.LogError($"Искомый атрибут триггера отсутствует: {Name} {triggerAttributeName}"); return; }
                Attribute ispAttribute = ispElement.Attributes.Points.Item(triggerAttributeName);
                if (!(ispAttribute.ValueType == AttributeValueType.TSDB))
                {
                    _logger.LogError($"Тип атрибута триггера не TSDB: {Name} {triggerAttributeName} {ispAttribute.ValueType}");
                    return;
                }
                if (string.IsNullOrEmpty(ispAttribute.Value))
                { _logger.LogError($"Пустое значение тега подписки: {Name} {triggerAttributeName} {ispAttribute.Value}"); return; }
                IsTriggerSchedulle = true;
                CalcAttribute item = new()
                {
                    Name = ispAttribute.Name
                };
                item.OutDataSource.Type = AttributeValueType.TSDB;
                item.OutDataSource.Name = ispAttribute.Value;
                TriggerAttributes.Add(item);
            }
        }
        private void Sort()
        {
            bool sorted = false;
            while (!sorted)
            {
                sorted = true;
                bool changed = false;
                for (int i = 0; i < Attributes.Count; i++) //Определение переменных из атрибутов CENG.Формула
                {
                    for (int j = 0; j < Attributes.Count; j++)
                    {
                        changed = MapCalcAttributeAndChangeOrder(i, j);
                        if (changed) { sorted = false; break; }
                    }
                    if (changed) { break; }
                }
            }
            if (Attributes.Count > 1)
            {
                for (int order = Attributes.Min(item => item.Order); order <= Attributes.Max(item => item.Order); order++)
                {
                    List<CalcAttribute> calcItems = Attributes.ToArray().Where(item => item.Order == order).ToList();
                    Queue.Add(calcItems);
                }
            }
            else if (Attributes.Count == 1)
            {
                List<CalcAttribute> calcItems = new() { Attributes.ToArray()[0] };
                Queue.Add(calcItems);
            }
        }
        private bool MapCalcAttributeAndChangeOrder(int i, int j)
        {
            CalcAttribute itemJ = Attributes.ToArray()[j];
            CalcAttribute itemI = Attributes.ToArray()[i];
            string path = "[" + itemJ.Name + "]";
            bool changed = false;
            if (string.IsNullOrEmpty(itemJ.Variable))
            {
                itemJ.Variable = Interpreter.GetVariableName();
            }
            if (itemI.Expression.Contains(path))
            {
                itemI.Expression = itemI.Expression.Replace(path, itemJ.Variable);
            }
            if (!string.IsNullOrEmpty(itemI.OutDataSource.Time))
            {
                if (itemI.OutDataSource.Time.Contains(path))
                {
                    itemI.OutDataSource.Time = itemI.OutDataSource.Time.Replace(path, itemJ.Variable);
                }
            }
            if (itemI.Expression.Contains(itemJ.Variable))
            {
                if (!(itemI.Order > itemJ.Order))
                {
                    itemI.Order = itemJ.Order + 1;
                    changed = true;
                }
            }
            return changed;
        }
    }
}
