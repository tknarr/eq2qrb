using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;
using System.Xml;

//  ProfitUI Quick Raid Buttons tool
//  Copyright (C) 2013 Todd T Knarr <tknarr@silverglass.org> <tknarr@cox.net> <todd.knarr@gmail.com>

//  This program is free software: you can redistribute it and/or modify
//  it under the terms of the GNU General Public License as published by
//  the Free Software Foundation, either version 3 of the License, or
//  (at your option) any later version.

//  This program is distributed in the hope that it will be useful,
//  but WITHOUT ANY WARRANTY; without even the implied warranty of
//  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
//  GNU General Public License for more details.

//  You should have received a copy of the GNU General Public License
//  along with this program.  If not, see <http://www.gnu.org/licenses/>.

namespace QuickRaidButtons
{
    // Service ID for Census API: eq2qrb

    public class EQ2DataFeed
    {
        private class QSParam
        {
            public string Var { get; set; }
            public string Val { get; set; }

            public QSParam()
            {
                Var = "";
                Val = "";
            }

            public QSParam( string a, string b )
            {
                Var = a;
                Val = b;
            }
        }

        public class SpellInfo : IComparable<SpellInfo>, ICloneable
        {
            public long SpellID { get; set; }
            public string ClassName { get; set; }
            public string SpellName { get; set; }
            public int Level { get; set; }

            public SpellInfo()
            {
                SpellID = 0;
                ClassName = "";
                SpellName = "";
                Level = 0;
            }

            public SpellInfo( long sid, string cn, string sn, int lvl )
            {
                SpellID = sid;
                ClassName = cn;
                SpellName = sn;
                Level = lvl;
            }

            #region IComparable<SpellInfo> Members

            public int CompareTo( SpellInfo other )
            {
                return SpellID.CompareTo( other.SpellID );
            }

            #endregion

            #region ICloneable Members

            public SpellInfo Clone()
            {
                return new SpellInfo( SpellID, ClassName, SpellName, Level );
            }

            object ICloneable.Clone()
            {
                return Clone();
            }

            #endregion
        }

        private string host_name = "data.soe.com";
        private string service_id = "eq2qrb";

        private string makeQueryString( List<QSParam> qsp )
        {
            StringBuilder sb = new StringBuilder( "" );

            foreach ( QSParam p in qsp )
            {
                if ( !String.IsNullOrEmpty( p.Var ) )
                {
                    if ( sb.Length > 0 )
                        sb.Append( "&" );
                    sb.Append( p.Var );
                    if ( !String.IsNullOrEmpty( p.Val ) )
                    {
                        sb.Append( "=" );
                        sb.Append( System.Uri.EscapeDataString( p.Var ) );
                    }
                }
            }

            return sb.ToString();
        }

        private string makeURL( string command, string collection, string identifier, List<QSParam> qsp )
        {
            string query_string = makeQueryString( qsp );
            return "http://" + host_name + ( !String.IsNullOrEmpty( service_id ) ? ( "/s:" + service_id ) : "" ) + "/xml/" + command + "/" + collection +
                ( !String.IsNullOrEmpty( identifier ) ? ( "/" + identifier ) : "" ) +
                ( !String.IsNullOrEmpty( query_string ) ? ( "?" + query_string ) : "" );
        }

        // Data XML:
        // <spell_list limit="10" min_ts="1363706046.077499" returned="10" seconds="0.023062">
        //     <spell alternate_advancement="0" aoe_radius_meters="0" beneficial="0" cast_secs_hundredths="40" chardiff="0"
        //            crc="1508415385" deity="0" description="A kick attack that deals crushing damage to the target."
        //            description_pvp="A kick attack that deals crushing damage to the target." given_by="spellscroll" id="3245071695"
        //            last_update="1363706056.775391" level="94" max_targets="0" name="Knee Break VIII" name_lower="knee break viii" recast_secs="9.881423"
        //            recovery_secs_tenths="50" spellbook="1" tier="9" tier_name="Master" ts="1363706056.775391" type="arts" typeid="1" version="1">
        //         <cost concentration="0" health="0" power="75" savagery="0">
        //             <per_tick health="0" power="0" savagery="0"/>
        //         </cost>
        //         <duration does_not_expire="0" max_sec_tenths="0" min_sec_tenths="0"/>
        //         <icon backdrop="315" icon_heroic_op="4" id="292"/>
        //         <classes>
        //             <berserker displayname="Berserker" id="4" level="94"/>
        //         </classes>
        //         <effect_list>
        //             <effect description="Inflicts 316 - 949 crushing damage on target" indentation="0"/>
        //         </effect_list>
        //     </spell>
        // </spell_list>

        public List<SpellInfo> getSpellByName( string class_name, string spell_name )
        {
            string snlower = spell_name.ToLower();
            string cnlower = class_name.ToLower();
            List<QSParam> qsp = new List<QSParam>();
            qsp.Add( new QSParam( "c:limit", "20" ) );
            qsp.Add( new QSParam( "name_lower", spell_name ) );
            qsp.Add( new QSParam( "c:has", "classes." + cnlower + ".displayname" ) );
            string query_url = makeURL( "get", "spell", "", qsp );

            HttpWebRequest web_req = WebRequest.Create( query_url ) as HttpWebRequest;
            if ( web_req == null )
                return null;
            HttpWebResponse web_resp = null;
            try
            {
                web_resp = web_req.GetResponse() as HttpWebResponse;
            }
            catch ( Exception e )
            {
                throw new QRBException( "Exception encountered getting data from SOE", e );
            }
            if ( web_resp == null )
            {
                throw new QRBException( "No response to request for data from SOE" );
            }

            XmlReaderSettings settings = new XmlReaderSettings();
            settings.IgnoreWhitespace = true;
            settings.IgnoreComments = true;
            settings.CloseInput = true;
            XmlDocument doc = null;
            try
            {
                using ( XmlReader reader = XmlReader.Create( web_resp.GetResponseStream(), settings ) )
                {
                    try
                    {
                        doc.Load( reader );
                    }
                    catch ( Exception e )
                    {
                        throw new QRBException( "Exception parsing EQ2 data XML", e );
                    }
                }
                web_resp.Close();
            }
            catch ( Exception e )
            {
                throw new QRBException( "Exception reading EQ2 data response", e );
            }
            XmlElement root = ( doc != null ) ? doc.DocumentElement : null;
            if ( root == null )
            {
                throw new QRBException( "No data in EQ2 data response" );
            }

            List<SpellInfo> ret = new List<SpellInfo>();

            foreach ( XmlNode n in root.ChildNodes )
            {
                XmlElement e = n as XmlElement;
                if ( e == null || e.LocalName != "spell" )
                    continue;

                long crc = 0;
                int level = 0;

                string s = e.GetAttribute( "crc" );
                if ( !String.IsNullOrEmpty( s ) )
                {
                    if ( !long.TryParse( s, out crc ) )
                        crc = 0;
                }
                s = e.GetAttribute( "level" );
                if ( !String.IsNullOrEmpty( s ) )
                {
                    if ( !int.TryParse( s, out level ) )
                        level = 0;
                }

                foreach ( XmlNode n1 in e.ChildNodes )
                {
                    XmlElement e1 = n1 as XmlElement;
                    if ( e1 == null || e1.LocalName != "classes" )
                        continue;

                    foreach ( XmlNode n2 in e1.ChildNodes )
                    {
                        XmlElement e2 = n2 as XmlElement;
                        if ( e2 == null )
                            continue;
                        string cn = e2.GetAttribute( "displayname" );
                        if ( !String.IsNullOrEmpty( cn ) )
                        {
                            SpellInfo i = new SpellInfo( crc, cn, spell_name, level );
                            if ( !ret.Contains( i ) )
                                ret.Add( i );
                        }
                    }
                }
                if ( ret.Count == 0 )
                {
                    SpellInfo i = new SpellInfo( crc, class_name, spell_name, level );
                    ret.Add( i );
                }
            }

            return ret;
        }

        public SpellInfo getSpellByID( long spell_id )
        {
            List<QSParam> qsp = new List<QSParam>();
            qsp.Add( new QSParam( "c:limit", "20" ) );
            qsp.Add( new QSParam( "crc", spell_id.ToString() ) );
            string query_url = makeURL( "get", "spell", "", qsp );

            HttpWebRequest web_req = WebRequest.Create( query_url ) as HttpWebRequest;
            if ( web_req == null )
                return null;
            HttpWebResponse web_resp = null;
            try
            {
                web_resp = web_req.GetResponse() as HttpWebResponse;
            }
            catch ( Exception e )
            {
                throw new QRBException( "Exception encountered getting data from SOE", e );
            }
            if ( web_resp == null )
            {
                throw new QRBException( "No response to request for data from SOE" );
            }

            XmlReaderSettings settings = new XmlReaderSettings();
            settings.IgnoreWhitespace = true;
            settings.IgnoreComments = true;
            settings.CloseInput = true;
            XmlDocument doc = null;
            try
            {
                using ( XmlReader reader = XmlReader.Create( web_resp.GetResponseStream(), settings ) )
                {
                    try
                    {
                        doc.Load( reader );
                    }
                    catch ( Exception e )
                    {
                        throw new QRBException( "Exception parsing EQ2 data XML", e );
                    }
                }
                web_resp.Close();
            }
            catch ( Exception e )
            {
                throw new QRBException( "Exception reading EQ2 data response", e );
            }
            XmlElement root = ( doc != null ) ? doc.DocumentElement : null;
            if ( root == null )
            {
                throw new QRBException( "No data in EQ2 data response" );
            }

            // TODO

            return null;
        }
    }
}
