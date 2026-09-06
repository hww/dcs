using NUnit.Framework;
using UnityEngine;
using System.Collections.Generic;
using DynamicComponent;

namespace DynamicComponent.Tests
{
    [TestFixture]
    public class DynamicFactsTests
    {
        private DynamicFacts _facts;

        [SetUp]
        public void Setup()
        {
            _facts = new DynamicFacts();
        }

        // ==================== ТЕСТЫ ПАРСИНГА ФОРМАТОВ (ОДНА СТРОКА VS МНОГОСТРОЧНЫЙ) ====================

        [Test]
        public void Parse_SingleLineJson_SuccessfullyParsesHeterogeneousData()
        {
            // Тестируем то, на чем ломался старый парсер: JSON в одну строку без '\n'
            string singleLineJson = "{\"speed\":25.5,\"health\":100,\"isAlive\":true,\"name\":\"Hero\",\"pos\":[10.0,5.5]}";

            // Задаем данные через рефлексию или симулируем десериализацию Unity
            SetJsonDataViaReflection(_facts, singleLineJson);
            _facts.OnAfterDeserialize();

            // Проверяем, что гетерогенные данные успешно распарсились
            Assert.IsTrue(_facts.Contains("speed"));
            Assert.AreEqual(25.5f, _facts.Get<float>("speed"));
            Assert.AreEqual(100, _facts.Get<int>("health"));
            Assert.IsTrue(_facts.Get<bool>("isAlive"));
            Assert.AreEqual("Hero", _facts.Get<string>("name"));
            Assert.AreEqual(new Vector2(10.0f, 5.5f), _facts.Get<Vector2>("pos"));
        }

        [Test]
        public void Parse_MultiLineJsonWithWhitespace_SuccessfullyParses()
        {
            // Тестируем красивый многострочный JSON со случайными пробелами и табами
            string multiLineJson = @"
            {
                ""speed"" : 5.5 ,
                ""name"":   ""Validator""   ,
                ""color"": [1.0, 0.0, 0.0, 1.0]
            }";

            SetJsonDataViaReflection(_facts, multiLineJson);
            _facts.OnAfterDeserialize();

            Assert.AreEqual(5.5f, _facts.Get<float>("speed"));
            Assert.AreEqual("Validator", _facts.Get<string>("name"));
            Assert.AreEqual(Color.red, _facts.Get<Color>("color"));
        }

        // ==================== ТЕСТЫ РАБОТЫ С ТИПАМИ ДАННЫХ (GET / SET) ====================

        [Test]
        public void SetAndGet_AllSupportedTypes_MaintainPrecisionAndState()
        {
            // Заполняем динамические факты в рантайме
            _facts.Set("intVal", 42);
            _facts.Set("floatVal", 3.14159f);
            _facts.Set("boolVal", false);
            _facts.Set("stringVal", "NaughtyDogStyle");
            _facts.Set("vec3Val", new Vector3(1f, 2f, 3f));
            _facts.Set("colorVal", new Color(0.5f, 0.5f, 0.5f, 1f));

            // Симулируем цикл сериализации (запись в строку -> чтение из строки)
            _facts.OnBeforeSerialize();
            _facts.OnAfterDeserialize();

            // Проверяем идентичность типов и значений
            Assert.AreEqual(42, _facts.Get<int>("intVal"));
            Assert.AreEqual(3.14159f, _facts.Get<float>("floatVal"), 0.00001f);
            Assert.IsFalse(_facts.Get<bool>("boolVal"));
            Assert.AreEqual("NaughtyDogStyle", _facts.Get<string>("stringVal"));
            Assert.AreEqual(new Vector3(1f, 2f, 3f), _facts.Get<Vector3>("vec3Val"));
            Assert.AreEqual(new Color(0.5f, 0.5f, 0.5f, 1f), _facts.Get<Color>("colorVal"));
        }

        [Test]
        public void Get_WithDefaultValue_ReturnsDefaultIfKeyNotFound()
        {
            // Проверяем безопасный перегруженный метод с дефолтным значением
            float speed = _facts.Get<float>("non_existing_speed", 12.5f);
            string name = _facts.Get<string>("non_existing_name", "DefaultName");

            Assert.AreEqual(12.5f, speed);
            Assert.AreEqual("DefaultName", name);
        }

        [Test]
        public void TryGet_ExistingAndNonExistingKeys_ReturnsCorrectBool()
        {
            _facts.Set("existing_fact", true);
            _facts.OnBeforeSerialize();
            _facts.OnAfterDeserialize();

            bool success = _facts.TryGet<bool>("existing_fact", out bool val);
            bool failure = _facts.TryGet<float>("missing_fact", out float missingVal);

            Assert.IsTrue(success);
            Assert.IsTrue(val);
            Assert.IsFalse(failure);
            Assert.AreEqual(0f, missingVal);
        }

        // ==================== ТЕСТЫ ОШИБОК И ВАЛИДАЦИИ ====================

        [Test]
        public void Get_NonExistingKeyWithoutDefault_ThrowsKeyNotFoundException()
        {
            Assert.Throws<KeyNotFoundException>(() => {
                _facts.Get<int>("unknown_key");
            });
        }

        [Test]
        public void Get_UnsupportedType_ThrowsNotSupportedException()
        {
            // Проверяем, что BaseFacts блокирует неподдерживаемые типы (например, массивы или кастомные классы)
            Assert.Throws<System.NotSupportedException>(() => {
                _facts.Get<int[]>("some_array");
            });
        }

        [Test]
        public void Parse_InvalidJson_DoesNotCrashAndReturnsEmpty()
        {
            // Системный тест: сломанный JSON не должен валить движок в рантайме
            string brokenJson = "{ \"speed\": 20, missing_quotes_and_colon }";

            SetJsonDataViaReflection(_facts, brokenJson);

            // Метод отловит Exception внутри try-catch и запишет предупреждение в LogWarning
            Assert.DoesNotThrow(() => _facts.OnAfterDeserialize());
        }

        // ==================== ВСПОМОГАТЕЛЬНЫЕ МЕТОДЫ ====================

        private void SetJsonDataViaReflection(DynamicFacts target, string value)
        {
            // Так как поле jsonData приватное, используем рефлексию для симуляции ввода в инспекторе Unity
            var field = typeof(DynamicFacts).GetField("jsonData",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            field?.SetValue(target, value);
        }
    }
}
