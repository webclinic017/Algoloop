//  Copyright (C) 2012 Capnode AB

//  This program is free software: you can redistribute it and/or modify
//  it under the terms of the GNU General Public License as published by
//  the Free Software Foundation, either version 2 of the License, or
//  (at your option) any later version.

//  This program is distributed in the hope that it will be useful,
//  but WITHOUT ANY WARRANTY; without even the implied warranty of
//  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
//  GNU General Public License for more details.

//  You should have received a copy of the GNU General Public License
//  along with this program.  If not, see <http://www.gnu.org/licenses/>.

using System;
using System.Collections.Generic;
using System.IO;

namespace Algoloop
{
  public class MsDirectory
  {
    string _path;

    public MsDirectory(string path)
    {
      _path = path;
    }

    public List<MsSecurity> GetSecurities()
    {
      List<MsSecurity> securities = new List<MsSecurity>();
      string filePath = _path + @"\MASTER";
      if (File.Exists(filePath))
      {
        FileStream fs = new FileStream(filePath, FileMode.Open, FileAccess.Read);
        BinaryReader br = new BinaryReader(fs);
        try
        {
          long size = fs.Length;
          long pos = 0;
          MsSecurity security = new MsSecurity(_path);
          pos += security.ReadHeader(br);
          while (pos < size)
          {
            security = new MsSecurity(_path);
            pos += security.Read(br);
            securities.Add(security);
          }
        }
        catch (Exception e)
        {
          Console.WriteLine(e.ToString());
        }
        finally
        {
          br.Close();
        }
      }
      securities.Sort(); // Sort list by Name
      return securities;
    }

    public MsSecurity GetSecurity(string symbol)
    {
      string filePath = _path + @"\MASTER";
      if (File.Exists(filePath))
      {
        FileStream fs = new FileStream(filePath, FileMode.Open, FileAccess.Read);
        BinaryReader br = new BinaryReader(fs);
        try
        {
          long size = fs.Length;
          long pos = 0;
          MsSecurity security = new MsSecurity(_path);
          pos += security.ReadHeader(br);
          while (pos < size)
          {
            security = new MsSecurity(_path);
            pos += security.Read(br);
            if (security.Symbol == symbol)
              return security;
          }
        }
        catch (Exception e)
        {
          Console.WriteLine(e.ToString());
        }
        finally
        {
          br.Close();
        }
      }
      return null;
    }
  }
}
