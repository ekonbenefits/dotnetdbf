import java.io.*;
import junit.framework.*;
import junit.framework.Test.*;
import com.linuxense.javadbf.*;

public class JavaDBFTest extends TestCase  {

	public JavaDBFTest( String s) {

		super( s);
	}

	private void print( String s) {

		System.out.print( s);
	}

	private void println( String s) {

		System.out.println( s);
	}

	public void test1() throws Exception {

		print( "Creating an empty DBFWriter object... ");
		DBFWriter writer = new DBFWriter();
		println( "OK.");
	}	

	public void test2() throws Exception {
		print( "Creating an empty DBFField object... ");
		DBFField field = new DBFField();
		println( "OK.");
	}

	public void test3() throws Exception {

		print( "Writing a sample DBF file ... ");
		DBFField field = new DBFField();
		field.setName( "F1");
		field.setDataType( DBFField.FIELD_TYPE_N);
		DBFWriter writer = new DBFWriter();
		writer.setFields( new DBFField[] { field});
		writer.addRecord( new Object[] {new Double( 3)});
		FileOutputStream fos = new FileOutputStream( "/tmp/121212.dbf");
		writer.write( fos);
		fos.flush();
		fos.close();
		println( "OK.");
	}

	public void test4() throws Exception {

		print( "Reading the written file ...");
		FileInputStream fis = new FileInputStream( "/tmp/121212.dbf");
		DBFReader reader = new DBFReader( fis);
		print( "\tRecord count=" + reader.getRecordCount());
		fis.close();
		println( " OK.");
	}

	public void checkDataType_N() throws Exception {

		FileOutputStream fos = new FileOutputStream( "/tmp/121212.dbf");
		DBFWriter writer = new DBFWriter();
		DBFField field = new DBFField();
		field.setName( "F1");
		field.setDataType( DBFField.FIELD_TYPE_N);
		field.setFieldLength( 15);
		field.setDecimalCount( 0);

		writer.setFields( new DBFField[] { field});
		Double value = new Double( 123456789012345L);
		writer.addRecord( new Object[] { value});
		print( " written=" + value);
		writer.write( fos);
		fos.close();

		FileInputStream fis = new FileInputStream( "/tmp/121212.dbf");
		DBFReader reader = new DBFReader( fis);

		Object[] values = reader.nextRecord();
		print( " read=" + (Double)values[0]);
		println( " written == read (" + (((Double)values[0]).equals( value)) + ")");
		fis.close();
	}

	public void checkRAFwriting() throws Exception {

		print( "Writing in RAF mode ... ");
		File file = new File( "/tmp/raf-1212.dbf");
		if( file.exists()) {

			file.delete();
		}
		DBFWriter writer = new DBFWriter( file);

		DBFField []fields = new DBFField[2];
		
		fields[0] = new DBFField();
		fields[0].setName( "F1");
		fields[0].setDataType( DBFField.FIELD_TYPE_C);
		fields[0].setFieldLength( 10);

		fields[1] = new DBFField();
		fields[1].setName( "F2");
		fields[1].setDataType( DBFField.FIELD_TYPE_N);
		fields[1].setFieldLength( 2);

		writer.setFields( fields);

		Object []record = new Object[2];
		record[0] = "Red";
		record[1] = new Double( 10);

		writer.addRecord( record);

		record = new Object[2];
		record[0] = "Blue";
		record[1] = new Double( 20);

		writer.addRecord( record);

		writer.write();
		println( "done.");

		print( "Appending to this file");

		writer = new DBFWriter( file);

		record = new Object[2];
		record[0] = "Green";
		record[1] = new Double( 33);

		writer.addRecord( record);

		record = new Object[2];
		record[0] = "Yellow";
		record[1] = new Double( 44);

		writer.addRecord( record);

		writer.write();
		println( "done.");

	}

	public static Test suite() {

		TestSuite s = new TestSuite();
		s.addTest( new JavaDBFTest( "test1"));
		s.addTest( new JavaDBFTest( "test2"));
		s.addTest( new JavaDBFTest( "test3"));
		s.addTest( new JavaDBFTest( "test4"));
		s.addTest( new JavaDBFTest( "checkDataType_N"));
		s.addTest( new JavaDBFTest( "checkRAFwriting"));

		return s;
	}
}
