using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace NeuralNetwork
{
    public class Neuron
    {
        private Random rnd = new Random();

        private int inputIndex;

        private int function = 1;   //  Función de activación por defecto Hardlim

        private double B;

        private double[] W;

        private double Sum;


        //  Constructor de neurona para la creación de la misma.
        public Neuron(int Number_inputs, int function)
        {
            this.W = new double[Number_inputs];
            this.inputIndex = Number_inputs;
            this.function = function;
            B = rnd.Next(-300, 300) / 100.0;

            for (int i = 0; i < inputIndex; i++)
            {
                W[i] = rnd.Next(-200, 200) / 100.0;
            }
        }

        //  Constructor para establecer neurona ya entrenada
        public Neuron(int Number_inputs, List<double> W, double B, int function)
        {
            if (Number_inputs != W.Count)
                throw new System.ArgumentOutOfRangeException("El número de entradas no coincide con el número de elementos de la lista (W).");

            this.inputIndex = Number_inputs;
            this.function = function;
            this.B = B;

            for (int i = 0; i < inputIndex; i++)
            {
                this.W[i] = W.ElementAt(i);
            }
        }

        //  Constructor para establecer neurona ya entrenada
        public Neuron(int Number_inputs, double[] W, double B, int function)
        {
            if (Number_inputs != W.Length)
                throw new System.ArgumentOutOfRangeException("El número de entradas no coincide con el número de elementos de la lista (W).");

            this.inputIndex = Number_inputs;
            this.function = function;
            this.B = B;

            for (int i = 0; i < inputIndex; i++)
            {
                this.W[i] = W.ElementAt(i);
            }
        }

        //  Método calcular Valor neto de la neurona
        public double getWorth(double[] inputs)
        {
            if (inputs.Length != W.Length)
                throw new System.ArgumentOutOfRangeException("El número de elementos del vector (W) no coincide con el número de elementos de (P).");

            Sum = 0;

            for (int i = 0; i < inputIndex; i++)
            {
                Sum += W[i] * inputs[i];
            }

            Sum += B;

            return Sum;
        }

        //  Método calcular función de la neurona F(Sum)
        public double getAxis(double[] inputs)
        {
            getWorth(inputs);

            if (function == 1)
            {
                if (Sum >= 0)
                    return 1;
                else return 0;
            }
            else return Sum;
        }

        //  Metodo que devuelve pesos sinápticos W[]
        public double[] getW()
        {
            return W;
        }

        public void resetNeuron(int from, int to)
        {
            B = rnd.Next(from * 100, to * 100) / 100.0;

            for (int i = 0; i < inputIndex; i++)
            {
                W[i] = rnd.Next(from * 100, to * 100) / 100.0;
            }
        }

        public void setB(double Bias)
        {
            B = Bias;
        }

        public void setAW(double W, int IndexInput)
        {
            this.W[IndexInput] = W;
        }

        public void setW(double[] W)
        {

            if (W.Length != this.W.Length)
                throw new ArgumentOutOfRangeException();

            this.W = W;
        }

        //  Metodo que devuelve pesos sinápticos usando un parámetro como índice el cual devolverá W[x] (índice de entrada a la neurona)
        public double getAW(int index)
        {
            return W[index];
        }

        //  Metodo que devuelve Bias
        public double getB()
        {
            return B;
        }
        public int getInputIndex()
        {
            return inputIndex;
        }
    }

    public class Agent
    {
        public Layer[] neuronas = new Layer[3];

        private Neuron[] neurona_1;
        private Neuron[] neurona_2;
        private Neuron[] neurona_3;
        public bool isAlive;
        public decimal fitness;

        public const int Inputs = 32;
        public const int FuncionActivacion = 1;
        public const int N_neuronas1 = 32;
        public const int N_neuronas2 = 32;
        public const int N_neuronas3 = 4;


        public Agent()
        {
            fitness = 0;

            neurona_1 = new Neuron[N_neuronas1];
            neurona_2 = new Neuron[N_neuronas2];
            neurona_3 = new Neuron[N_neuronas3];

            isAlive = true;

            //  Instanciamiento aleatorio de neuronas
            for (int i = 0; i < N_neuronas1; i++)
            {
                neurona_1[i] = new Neuron(Inputs, FuncionActivacion);
                //neurona_1[i].resetNeuron(rnd.Next(-3, 0), rnd.Next(0, 3));
            }
            for (int i = 0; i < N_neuronas2; i++)
            {
                neurona_2[i] = new Neuron(N_neuronas1, FuncionActivacion);
                //neurona_2[i].resetNeuron(rnd.Next(-3, 0), rnd.Next(0, 3));
            }
            for (int i = 0; i < N_neuronas3; i++)
            {
                neurona_3[i] = new Neuron(N_neuronas2, FuncionActivacion);
                //neurona_3[i].resetNeuron(rnd.Next(-3, 0), rnd.Next(0, 3));
            }


            neuronas[0] = new Layer(neurona_1);
            neuronas[1] = new Layer(neurona_2);
            neuronas[2] = new Layer(neurona_3);


            neurona_1 = null;
            neurona_2 = null;
            neurona_3 = null;


        }

    }


    //  Objeto tipo capa que contiene objetos de tipo Neurona
    public class Layer
    {
        public Neuron[] N;
        int Index;
        double[] Axis;

        public Layer(Neuron[] N)
        {
            this.N = N;
            Index = N.Length;
            Axis = new double[N.Length];

            for(int i = 0; i < Axis.Length; i++)
            {
                Axis[i] = 0;
            }
        }

        public double[] getAllAxis(double[] inputs)
        {
            for (int i = 0; i < Index; i++)
            {
                Axis[i] = N[i].getAxis(inputs);
            }

            return Axis;
        }
    }

    public static class Models
    {
        
    }

    
}
