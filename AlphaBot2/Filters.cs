namespace Filters
{
    /*A simplified one dimensional Kalman filter implementation - actually a single variable low pass filter */

    internal class Kalman
    {
        /* Kalman filter variables */
        private double q; //process noise covariance
        private double r; //measurement noise covariance
        private double x; //value
        private double p; //estimation error covariance
        private double k; //kalman gain

        public Kalman(double process_noise = 0.125, double sensor_noise = 32, double estimated_error = 1023, double intial_value = 0)
        {
            /*
                The variables are x for the filtered value, q for the process noise,
                r for the sensor noise, p for the estimated error and k for the Kalman Gain.
                The state of the filter is defined by the values of these variables.

                The initial values for p is not very important since it is adjusted
                during the process. It must be just high enough to narrow down.
                The initial value for the readout is also not very important, since
                it is updated during the process.
                But tweaking the values for the process noise and sensor noise
                is essential to get clear readouts.

                q = 0.125
                r = 32
                p = 1023 //"large enough to narrow down"
                e.g.
                myVar = Kalman(0.125,32,1023,0);
            */
            this.q = process_noise;
            this.r = sensor_noise;
            this.p = estimated_error;
            this.x = intial_value; //x will hold the iterated filtered value
        }

        public double getFilteredValue(double measurement)
        {
            /* Updates and gets the current measurement value */
            //prediction update
            //omit x = x
            this.p = this.p + this.q;

            //measurement update
            this.k = this.p / (this.p + this.r);
            this.x = this.x + this.k * (measurement - this.x);
            this.p = (1 - this.k) * this.p;

            return this.x;
        }

        public void setParameters(double process_noise, double sensor_noise, double estimated_error)
        {
            this.q = process_noise;
            this.r = sensor_noise;
            this.p = estimated_error;
        }

        public void setParameters(double process_noise, double sensor_noise)
        {
            this.q = process_noise;
            this.r = sensor_noise;
        }

        public double getProcessNoise()
        {
            return this.q;
        }

        public double getSensorNoise()
        {
            return this.r;
        }

        public double getEstimatedError()
        {
            return this.p;
        }
    };
}