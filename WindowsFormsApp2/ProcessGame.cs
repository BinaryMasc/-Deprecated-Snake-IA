using System;
using System.IO;
using System.Threading;
using System.Xml;

namespace WindowsFormsApp2
{
    public class Processess
    {
        public static void ProcessGameThread(Agente agente)
        {

            //  Verificar que el agente esté con vida
            if (!agente.isAlive)
                return;

            //  Refrescar la matriz del ambiente del agente

            for (int i = 0; i < 77; i++)
            {
                for (int j = 0; j < 33; j++)
                {
                    agente.matrixScreen[i, j] = 1;
                }
            }


            //  Registrar en el ambiente la ubicación de la cabeza, comida y cola

            //int cabezax = checked(Math.Abs(agente.cabeza.x / 10));
            //int cabezay = checked(Math.Abs(agente.cabeza.y / 10));

            //int comidax = checked(Math.Abs(agente.comida.x / 10));
            //int comiday = checked(Math.Abs(agente.comida.y / 10));

            int cabezax = agente.cabeza.x / 10;
            int cabezay = agente.cabeza.y / 10;

            int comidax = agente.comida.x / 10;
            int comiday = agente.comida.y / 10;

            if (cabezax >= 77 || cabezay >= 33)
            {
                agente.isAlive = false;
                return;
            }
                

            agente.matrixScreen[cabezax, cabezay] = 2;
            agente.matrixScreen[comidax, comiday] = 0;

            

            agente.matrixScreen = agente.cabeza.dibujar(agente.matrixScreen);


            Form1.movimiento(agente);

            int[] vectorEntrada = new int[Agente.Inputs];
            int[] vectorSalida = new int[Agente.N_neuronas3];
            int[] vectemp;

            vectemp = Processess.calcularVectorEntradas2(agente, 10, 0);
            vectorEntrada[0] = vectemp[0];
            vectorEntrada[1] = vectemp[1];
            vectorEntrada[2] = vectemp[2];

            vectemp = Processess.calcularVectorEntradas2(agente, 10, 10);
            vectorEntrada[3] = vectemp[0];
            vectorEntrada[4] = vectemp[1];
            vectorEntrada[5] = vectemp[2];

            vectemp = Processess.calcularVectorEntradas2(agente, 10, -10);
            vectorEntrada[6] = vectemp[0];
            vectorEntrada[7] = vectemp[1];
            vectorEntrada[8] = vectemp[2];

            vectemp = Processess.calcularVectorEntradas2(agente, 0, -10);
            vectorEntrada[9] = vectemp[0];
            vectorEntrada[10] = vectemp[1];
            vectorEntrada[11] = vectemp[2];

            vectemp = Processess.calcularVectorEntradas2(agente, 0, 10);
            vectorEntrada[12] = vectemp[0];
            vectorEntrada[13] = vectemp[1];
            vectorEntrada[14] = vectemp[2];

            vectemp = Processess.calcularVectorEntradas2(agente, -10, 10);
            vectorEntrada[15] = vectemp[0];
            vectorEntrada[16] = vectemp[1];
            vectorEntrada[17] = vectemp[2];

            vectemp = Processess.calcularVectorEntradas2(agente, -10, 0);
            vectorEntrada[18] = vectemp[0];
            vectorEntrada[19] = vectemp[1];
            vectorEntrada[20] = vectemp[2];

            vectemp = Processess.calcularVectorEntradas2(agente, -10, -10);
            vectorEntrada[21] = vectemp[0];
            vectorEntrada[22] = vectemp[1];
            vectorEntrada[23] = vectemp[2];

            //  Se le pasa el vector por parámetro a la red neuronal para que el agente decida su próximo movimiento

            vectorSalida = agente.procesar(agente.ConvertToDouble(vectorEntrada));


            if (agente.cabeza.IsInXcoord)
            {
                if (vectorSalida[0] == 1)
                {
                    agente.cabeza.Ydir = Form1.cuadro * -1;
                    agente.cabeza.Xdir = 0;
                    agente.cabeza.IsInXcoord = false;
                    agente.cabeza.IsInYcoord = true;
                }
                else if (vectorSalida[1] == 1)
                {
                    agente.cabeza.Ydir = Form1.cuadro;
                    agente.cabeza.Xdir = 0;
                    agente.cabeza.IsInXcoord = false;
                    agente.cabeza.IsInYcoord = true;
                }
            }

            else if (agente.cabeza.IsInYcoord)
            {
                if (vectorSalida[2] == 1)
                {
                    agente.cabeza.Xdir = Form1.cuadro * -1;
                    agente.cabeza.Ydir = 0;
                    agente.cabeza.IsInYcoord = false;
                    agente.cabeza.IsInXcoord = true;
                }
                else if (vectorSalida[3] == 1)
                {
                    agente.cabeza.Xdir = Form1.cuadro;
                    agente.cabeza.Ydir = 0;
                    agente.cabeza.IsInYcoord = false;
                    agente.cabeza.IsInXcoord = true;
                }
            }


            //  Detectar colisión con comida

            if (agente.cabeza.addfood(agente.comida))
            {
                agente.comida.establecerComida();
                agente.cabeza.meter();
                agente.matrixScreen[agente.comida.x / 10, agente.comida.y / 10] = 0;

                if (Math.Abs(agente.contP - agente.limitepasosAI) < 1800)
                    agente.limitepasosAI += 200;
            }

            //  Detectar Colisión con cuerpo o paredes
            if (agente.cabeza.Collision() || Form1.colisioncuerpo(agente))
            {
                agente.isAlive = false;
                Form1.reiniciar(agente, true);
                return;
            }

            Form1.IAProcess(agente);

        }


        //  Método que carga toda la información genética de los agentes en un archivo
        public static void SaveModelsFile(Agente[] agente, string path)
        {

            using (StreamWriter sw = File.CreateText(path))
            {


                for (int i = 0; i < agente.Length; i++)
                {
                    sw.WriteLine("=================== Iniciando Agente {0} ==========================", i + 1);
                    for (int j = 0; j < agente[i].neuronas.Length; j++)
                    {
                        for(int k = 0; k < agente[i].neuronas[j].N.Length; k++)
                        {
                            for(int l = 0; l < agente[i].neuronas[j].N[k].getInputIndex(); l++)
                            {
                                sw.WriteLine($"Peso Sináptico[Neurona: {k} / Layer: {j},Entrada: {l}] = {agente[i].neuronas[j].N[k].getAW(l)};");
                            }

                            sw.WriteLine($"Polarización[Neurona: {k}/ Layer: {j}] = {agente[i].neuronas[j].N[k].getB()};");
                        }
                    }
                }

                sw.Flush();
                sw.Close();
                sw.Dispose();
            }
        }
        //  Método que carga toda la información genética de un agente en específico en un archivo
        public static void SaveModelFile(Agente agente, string path)
        {

            using (StreamWriter sw = File.CreateText(path))
            {
                for (int j = 0; j < agente.neuronas.Length; j++)
                {
                    for (int k = 0; k < agente.neuronas[j].N.Length; k++)
                    {
                        for (int l = 0; l < agente.neuronas[j].N[k].getInputIndex(); l++)
                        {
                            sw.WriteLine($"Peso Sináptico[Neurona: {k} / Layer: {j},Entrada: {l}] = {agente.neuronas[j].N[k].getAW(l)};");
                        }

                        sw.WriteLine($"Polarización[Neurona: {k}/ Layer: {j}] = {agente.neuronas[j].N[k].getB()};");
                    }
                }

                sw.Flush();
                sw.Close();
                sw.Dispose();
            }
        }

        public static void SaveModelFileXML(Agente agente, string path)
        {

            using (StreamWriter sw = File.CreateText(path))
            {
                sw.WriteLine("<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"no\"?>");
                sw.WriteLine($"<Agent>");

                for (int j = 0; j < agente.neuronas.Length; j++)
                {
                    sw.WriteLine($"\t<NS{j}>");
                    for (int k = 0; k < agente.neuronas[j].N.Length; k++)
                    {
                        sw.WriteLine($"\t\t<N{k}>");
                        for (int l = 0; l < agente.neuronas[j].N[k].getInputIndex(); l++)
                        {
                            sw.WriteLine($"\t\t\t<W{l}>{agente.neuronas[j].N[k].getAW(l)}</W{l}>");
                        }
                        sw.WriteLine($"\t\t\t<B>{agente.neuronas[j].N[k].getB()}</B>");
                        sw.WriteLine($"\t\t</N{k}>");
                    }
                    sw.WriteLine($"\t</NS{j}>");

                }

                sw.WriteLine($"</Agent>");

                sw.Flush();
                sw.Close();
                sw.Dispose();
            }
        }

        public static Agente[] LoadModelsFromXML(Agente[] agentes, int indexAgents, string path)
        {

            if (indexAgents != agentes.Length)
                throw new Exception("El número de agentes no coincide con el tamaño del arreglo.");

            XmlDocument doc = new XmlDocument();
            doc.Load(path);


            if(doc.DocumentElement.ChildNodes.Count != indexAgents)
                throw new Exception("El número de agentes no coincide con la lista de agentes del archivo XML.");

            int a = 0;  //  Iterador de agentes

            //  Recorrer agentes
            foreach (XmlNode node in doc.DocumentElement.ChildNodes)
            {
                //  Recorrer capas
                for(int i = 0; i < node.ChildNodes.Count; i++)
                {
                    //  Recorrer Neuronas
                    for (int j = 0; j < node.ChildNodes[i].ChildNodes.Count; j++)
                    {
                        //  Recorrer pesos
                        for(int k = 0; k < node.ChildNodes[i].ChildNodes[j].ChildNodes.Count; k++)
                        {
                            if (k == node.ChildNodes[i].ChildNodes[j].ChildNodes.Count - 1)
                                agentes[a].neuronas[i].N[j].setB(double.Parse(node.ChildNodes[i].ChildNodes[j].ChildNodes[k].InnerText));

                            else
                                agentes[a].neuronas[i].N[j].setAW(double.Parse(node.ChildNodes[i].ChildNodes[j].ChildNodes[k].InnerText), k);

                        }
                    }
                }

                a++;
            }

            return agentes;

        }

        public static Agente LoadModelFromXML(Agente agente,string path)
        {

            XmlDocument doc = new XmlDocument();
            doc.Load(path);


            //doc.DocumentElement.ChildNodes agentes
            //  Recorrer capas
            for (int i = 0; i < doc.DocumentElement.ChildNodes.Count; i++)
            {
                //  Recorrer Neuronas
                for (int j = 0; j < doc.DocumentElement.ChildNodes[i].ChildNodes.Count; j++)
                {
                    //  Recorrer pesos
                    for (int k = 0; k < doc.DocumentElement.ChildNodes[i].ChildNodes[j].ChildNodes.Count; k++)
                    {
                        if (k == doc.DocumentElement.ChildNodes[i].ChildNodes[j].ChildNodes.Count - 1)
                            agente.neuronas[i].N[j].setB(double.Parse(doc.DocumentElement.ChildNodes[i].ChildNodes[j].ChildNodes[k].InnerText));

                        else
                            agente.neuronas[i].N[j].setAW(double.Parse(doc.DocumentElement.ChildNodes[i].ChildNodes[j].ChildNodes[k].InnerText), k);

                    }
                }
            }

            return agente;

        }

        public static int[] calcularVectorEntradas(Agente agente, int variacionX, int variacionY)
        {

            int contemp = 1;
            int[] returner = new int[4];
            int xtemp = 0;
            int ytemp = 0;

            bool notColision = true;

            while (notColision)
            {
                //  Inicialización de variables de acceso a la matríz
                xtemp = (agente.cabeza.verX() / 10) + ((variacionX / 10) * contemp);
                ytemp = (agente.cabeza.verY() / 10) + ((variacionY / 10) * contemp);
                //	Evalúa si la colisión es de pared
                if (xtemp >= agente.matrixScreen.GetLength(0) || xtemp < 0 || ytemp < 0 || ytemp >= agente.matrixScreen.GetLength(1))
                {
                    notColision = false;
                    returner[2] = 1;
                    continue;
                }


                if (agente.matrixScreen[xtemp, ytemp] == 1)
                {

                    agente.matrixScreen[xtemp, ytemp] = 9;
                    contemp++;
                }


                if (agente.matrixScreen[xtemp, ytemp] == 2 || agente.matrixScreen[xtemp, ytemp] == 0)
                {
                    //	Evalúa si la colisión es de cola
                    if (agente.matrixScreen[xtemp, ytemp] == 2)
                    {
                        //MessageBox.Show($"cola detectada x: {xtemp}, y: {ytemp}");
                        agente.matrixScreen[xtemp, ytemp] = 8;
                        returner[3] = 1;
                    }
                    //	Evalúa si la colisión es de comida
                    else if (agente.matrixScreen[xtemp, ytemp] == 0)
                    {
                        //MessageBox.Show($"comida detectada x: {xtemp}, y: {ytemp}");
                        agente.matrixScreen[xtemp, ytemp] = 7;
                        returner[1] = 1;
                    }

                    notColision = false;

                }
            }

            returner[0] = contemp;

            return returner;
        }

        public static int[] calcularVectorEntradas2(Agente agente, int variacionX, int variacionY)
        {
            int contemp = 1;
            //  returner = [0]: Distancia a comida
            //  returner = [1]: Distancia a pared
            //  returner = [2]: Distancia a cuerpo
            int[] returner = new int[3];
            int xtemp = 0;
            int ytemp = 0;

            bool notColision = true;

            while (notColision)
            {
                //  Inicialización de variables de acceso a la matríz
                xtemp = (agente.cabeza.verX() / 10) + ((variacionX / 10) * contemp);
                ytemp = (agente.cabeza.verY() / 10) + ((variacionY / 10) * contemp);
                //	Evalúa si la colisión es de pared
                if (xtemp >= agente.matrixScreen.GetLength(0) || xtemp < 0 || ytemp < 0 || ytemp >= agente.matrixScreen.GetLength(1))
                {
                    notColision = false;
                    returner[0] = -1;
                    returner[1] = contemp;
                    returner[2] = -1;
                    continue;
                }


                if (agente.matrixScreen[xtemp, ytemp] == 1)
                {

                    agente.matrixScreen[xtemp, ytemp] = 9;
                    contemp++;
                }


                if (agente.matrixScreen[xtemp, ytemp] == 2 || agente.matrixScreen[xtemp, ytemp] == 0)
                {
                    //	Evalúa si la colisión es de cola
                    if (agente.matrixScreen[xtemp, ytemp] == 2)
                    {
                        //MessageBox.Show($"cola detectada x: {xtemp}, y: {ytemp}");
                        agente.matrixScreen[xtemp, ytemp] = 8;
                        returner[0] = -1;
                        returner[1] = -1;
                        returner[2] = contemp;
                    }
                    //	Evalúa si la colisión es de comida
                    else if (agente.matrixScreen[xtemp, ytemp] == 0)
                    {
                        //MessageBox.Show($"comida detectada x: {xtemp}, y: {ytemp}");
                        agente.matrixScreen[xtemp, ytemp] = 7;
                        returner[0] = contemp;
                        returner[1] = -1;
                        returner[2] = -1;
                    }

                    notColision = false;

                }
            }
            return returner;
        }
    }
}
