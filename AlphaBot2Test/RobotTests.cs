using NUnit.Framework;
using System;
using System.IO;
using AlphaBot2;
using System.Collections.Generic;
using System.Linq;

namespace AlphaBot2Test
{
    public class RobotTests
    {
        private AlphaBot2.AlphaBot2 _robot;
        [SetUp]
        public void Setup()
        {
            _robot = new AlphaBot2.AlphaBot2(new List<string>() { "adc", "10" });

        }

        [TestCase(new int[] { 50, 1000, 1000, 1000, 1000 }, ExpectedResult = -100)]
        [TestCase(new int[] { 50, 50, 1000, 1000, 1000 }, ExpectedResult = -75)]
        [TestCase(new int[] { 50, 50, 50, 1000, 1000 }, ExpectedResult = -50)]
        [TestCase(new int[] { 1000, 50, 50, 1000, 1000 }, ExpectedResult = -25)]
        [TestCase(new int[] { 1000, 50, 50, 50, 1000 }, ExpectedResult = 0)]
        [TestCase(new int[] { 50, 50, 50, 50, 50 }, ExpectedResult = 0)]
        [TestCase(new int[] { 1000, 1000, 50, 50, 50 }, ExpectedResult = 50)]
        [TestCase(new int[] { 1000, 1000, 1000, 50, 50 }, ExpectedResult = 75)]
        [TestCase(new int[] { 1000, 1000, 1000, 1000, 50 }, ExpectedResult = 100)]
        public double TestRangeADC(int[] valuesArray)
        {
            List<int> values = valuesArray.ToList();
            for (int i = 0; i < 2; i++)
            {
                _robot.FindLine(values);
            }
            var ADCLineValue = _robot.FindLine(values);
            var result = (ADCLineValue >= -100) & (ADCLineValue <= 100);
            Assert.IsTrue(result, "ADC Line Value out of Range");
            return (double)ADCLineValue;
        }
    }
}