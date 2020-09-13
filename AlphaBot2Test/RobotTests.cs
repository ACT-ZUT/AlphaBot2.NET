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
<<<<<<< Updated upstream
            _robot = new AlphaBot2.AlphaBot2(new List<string>() { "adc", "10" });

=======
            _robot = new AlphaBot();
            _robot.SetParameter(AlphaBot.Parameters.MainSpeed, 10);
>>>>>>> Stashed changes
        }

        [TestCase(new int[] { 50, 1000, 1000, 1000, 1000 }, ExpectedResult = -100)]
        [TestCase(new int[] { 50, 50, 1000, 1000, 1000 }, ExpectedResult = -75)]
        [TestCase(new int[] { 50, 50, 50, 1000, 1000 }, ExpectedResult = -50)]
        [TestCase(new int[] { 1000, 50, 50, 1000, 1000 }, ExpectedResult = -25)]
        [TestCase(new int[] { 1000, 50, 50, 50, 1000 }, ExpectedResult = 0)]
        [TestCase(new int[] { 50, 50, 50, 50, 50 }, ExpectedResult = 0)]
        [TestCase(new int[] { 1000, 1000, 50, 50, 1000 }, ExpectedResult = 25)]
        [TestCase(new int[] { 1000, 1000, 50, 50, 50 }, ExpectedResult = 50)]
        [TestCase(new int[] { 1000, 1000, 1000, 50, 50 }, ExpectedResult = 75)]
        [TestCase(new int[] { 1000, 1000, 1000, 1000, 50 }, ExpectedResult = 100)]
        public double TestADCtoLine(int[] valuesArray)
        {
            List<int> values = valuesArray.ToList();
            var ADCLineValue = _robot.FindLine(values);
            var result = (ADCLineValue >= -100) & (ADCLineValue <= 100);
            Assert.IsTrue(result, "ADC Line Value out of Range");
            return (double)ADCLineValue;
        }
<<<<<<< Updated upstream
=======

        [TestCase(100.0, ExpectedResult = 100)]
        [TestCase(30.0, ExpectedResult = 40)]
        [TestCase(10.0, ExpectedResult = 20)]
        [TestCase(0.0, ExpectedResult = 10)]
        [TestCase(-10.0, ExpectedResult = 0)]
        [TestCase(-30.0, ExpectedResult = -20)]
        [TestCase(-100.0, ExpectedResult = -90)]
        public double TestSetMotorSpeedL(double speed)
        {
            
            (var speedL, var speedR) = _robot.SetSpeed(speed);
            var result = (speedL >= -100) & (speedL <= 100);
            Assert.IsTrue(result, "ADC Line Value out of Range");
            return (double)speedL;
        }

        [TestCase(100.0, ExpectedResult = -90)]
        [TestCase(30.0, ExpectedResult = -20)]
        [TestCase(10.0, ExpectedResult = 0)]
        [TestCase(0.0, ExpectedResult = 10)]
        [TestCase(-10.0, ExpectedResult = 20)]
        [TestCase(-30.0, ExpectedResult = 40)]
        [TestCase(-100.0, ExpectedResult = 100)]
        public double TestSetMotorSpeedR(double speed)
        {
            (_, var speedR) = _robot.SetSpeed(speed);
            var result = (speedR >= -100) & (speedR <= 100);
            Assert.IsTrue(result, "ADC Line Value out of Range");
            return (double)speedR;
        }

        [TestCase(100.0, 0, ExpectedResult = 100)]
        [TestCase(-100.0, 0, ExpectedResult = -100)]
        [TestCase(30.0, 20, ExpectedResult = 50)]
        [TestCase(30.0, 50, ExpectedResult = 80)]
        public double TestChangeParameters(double speed, double mainSpeed)
        {
            _robot.SetParameter(AlphaBot.Parameters.MainSpeed, mainSpeed);
            (var speedL, _) = _robot.SetSpeed(speed);
            var result = (speedL >= -100) & (speedL <= 100);
            Assert.IsTrue(result, "ADC Line Value out of Range");
            return (double)speedL;
        }
>>>>>>> Stashed changes
    }
}