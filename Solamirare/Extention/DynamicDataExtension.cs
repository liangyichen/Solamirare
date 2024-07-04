namespace Solamirare;


/// <summary>
/// 动态数据保存，使用最常规的方式 new 对象，然后Save，这样有利于内存回收
/// </summary>
public sealed class DynamicDataExtensions
{


        /// <summary>
        /// 保存动态数据
        /// </summary>
        /// <param name="data"></param>
        /// <param name="iText"></param>
        /// <param name="dirPath"></param>
        /// <param name="filename"></param>
        /// <returns></returns>
        public async Task Save(ConcurrentDictionary<int,DynamicData> data,ITextSerializer iText,  string dirPath, string filename)
        {
            GeneralApplication.CheckDir(dirPath);

            var tempFileName = "temp_"+filename;
            var tempfile_fullpath = Path.Combine(dirPath,tempFileName);

            using (TextWriter writer = File.AppendText(tempfile_fullpath))  
            {  
                
                await writer.WriteAsync("{");
                    
                bool firstOrSingle = true;      
                
                foreach(var p in data)
                {
                    IEnumerable<KeyValuePair<string, string>> selected = p.Value.Export();
                    
                    var valueString = iText.SerializeObject(selected);

                    var keyString = p.Key.ToString();


                    if(firstOrSingle)
                    {
                        firstOrSingle = false;
                    }
                    else
                    {
                        await writer.WriteAsync(",");
                    }

                    await writer.WriteAsync("\"");
                    await writer.WriteAsync(keyString);
                    await writer.WriteAsync("\":{\"T\":");
                    await writer.WriteAsync(p.Value.T.ToString());
                    await writer.WriteAsync(",\"DomainId\":");
                    await writer.WriteAsync(p.Value.DomainId.ToString());
                    await writer.WriteAsync(",\"Data\":");
                    await writer.WriteAsync(valueString);
                    await writer.WriteAsync("}");


                    await writer.FlushAsync();
                    //var json = $"\"{keyString}\":{{\"T\":{p.Value.T},\"DomainId\":{p.Value.DomainId},\"Data\":{valueString}}}";
                   

                }

                await writer.WriteAsync("}");
                await writer.FlushAsync();

                writer.Close();
                writer.Dispose();
                
            }

            var new_filepath = Path.Combine(dirPath,filename);

            File.Delete(new_filepath);

            FileInfo fileInfo= new FileInfo(tempfile_fullpath);
            fileInfo.CopyTo(new_filepath);

            File.Delete(tempfile_fullpath);

            // GC.Collect();
            // GC.WaitForFullGCComplete();
        }


}